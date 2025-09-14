using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nuform.Core;

// Patched version of Estimator.cs implementing correct widthwise ceiling logic.
// This file is a copy of the user's original Estimator class with modifications
// to the ceiling calculation.  The widthwise branch no longer swaps length and
// width.  Instead, panels are oriented across the room width so that there is
// only one row.  The shipping length is determined based on the room width:
//  * If width > 25 ft the calculation falls back to the lengthwise algorithm.
//  * If width > 20 ft but <= 25 ft the shipping length is a custom value equal
//    to the width rounded up to the nearest foot.
//  * Otherwise the next even standard length (10, 12, 14, 16, 18 or 20 ft)
//    that is at least as long as the width is used.  The user‑supplied
//    CeilingPanelLengthFt is honoured only if it is an even length between
//    10 and 20 ft and is not shorter than the room width.
// Panels per row are computed along the room length and there is always one
// row when widthwise.  For lengthwise ceilings the original logic is
// preserved.
public class Estimator
{
    static double PanelWidthFt(double inches) => inches == 18 ? 1.5 : 1.0;

    static int RoundPanels(double panels)
    {
        if (panels <= 150)
            return (int)Math.Ceiling(panels / 2.0) * 2;
        return (int)Math.Ceiling(panels / 5.0) * 5;
    }

    public EstimateResult Estimate(EstimateInput input)
    {
        var result = new EstimateResult();
        result.Options = input.Options;
        result.Rooms = input.Rooms;
        var catalog = CatalogService.Load(input.Options.CatalogPdfPath);
        double netLF = 0;
        foreach (var room in input.Rooms)
        {
            netLF += 2 * (room.LengthFt + room.WidthFt);
        }
        if (input.Openings.Any())
        {
            var firstRoom = input.Rooms.FirstOrDefault();
            double panelWidthFt = PanelWidthFt(firstRoom?.PanelWidthInches ?? 12);
            double panelLenFt = firstRoom?.WallPanelLengthFt ?? 0;
            foreach (var op in input.Openings)
            {
                var piecesPerFull = panelLenFt / (op.HeaderHeight + op.SillHeight);
                var headerPanelsAdded = op.WidthFt / piecesPerFull;
                var headerLFAdded = headerPanelsAdded * panelWidthFt;
                netLF += (-op.WidthFt + headerLFAdded) * op.Count;
            }
        }
        double jtrimLF = 0, cornerLF = 0, baseLF = 0, crownLF = 0, topTrackLF = 0;
        if (input.Rooms.Any())
        {
            double panelWidthFt = PanelWidthFt(input.Rooms.First().PanelWidthInches);
            double panels = netLF / panelWidthFt;
            panels *= 1 + input.Options.Contingency;
            int rounded = RoundPanels(panels);
            double panelLen = input.Rooms.First().WallPanelLengthFt;
            result.WallPanels[panelLen] = rounded;

            double lengthChoice = panelLen <= 12 ? 12 : (panelLen <= 16 ? 16 : 16);
            string color = input.Options.Color;

            // J-trim around wall perimeter
            jtrimLF = netLF * (1 + input.Options.Contingency);
            var jItem = CatalogService.FindItem(catalog, "J-Trim", lengthChoice, color);
            if (jItem != null)
            {
                int packs = (int)Math.Ceiling(jtrimLF / jItem.LFPerPack);
                result.Trims.JTrimPacks = packs;
                result.Trims.JTrimPackLenFt = (int)lengthChoice;
                result.Parts.Add(new PartRequirement
                {
                    PartCode = jItem.PartCode,
                    QtyPacks = packs,
                    LFNeeded = jtrimLF,
                    TotalLFProvided = packs * jItem.LFPerPack
                });
            }

            // Corner trim: 4 corners per room
            cornerLF = input.Rooms.Sum(r => r.HeightFt * 4) * (1 + input.Options.Contingency);
            var cItem = CatalogService.FindItem(catalog, "Corner Trim", lengthChoice, color);
            if (cItem != null)
            {
                int packs = (int)Math.Ceiling(cornerLF / cItem.LFPerPack);
                result.Trims.CornerPacks = packs;
                result.Trims.CornerPackLenFt = (int)lengthChoice;
                result.Parts.Add(new PartRequirement
                {
                    PartCode = cItem.PartCode,
                    QtyPacks = packs,
                    LFNeeded = cornerLF,
                    TotalLFProvided = packs * cItem.LFPerPack
                });
            }

            // Crown/Base trims around perimeter
            baseLF = crownLF = netLF * (1 + input.Options.Contingency);
            var baseItem = CatalogService.FindItem(catalog, "Base Trim", lengthChoice, color);
            var crownItem = CatalogService.FindItem(catalog, "Crown Trim", lengthChoice, color);
            if (baseItem != null && crownItem != null)
            {
                int basePacks = (int)Math.Ceiling(baseLF / baseItem.LFPerPack);
                int crownPacks = (int)Math.Ceiling(crownLF / crownItem.LFPerPack);
                int packs = Math.Max(basePacks, crownPacks);
                result.Trims.CrownBasePairs = packs;
                result.Trims.TopTrackPackLenFt = (int)lengthChoice;
                result.Parts.Add(new PartRequirement
                {
                    PartCode = baseItem.PartCode,
                    QtyPacks = packs,
                    LFNeeded = baseLF,
                    TotalLFProvided = packs * baseItem.LFPerPack
                });
                result.Parts.Add(new PartRequirement
                {
                    PartCode = crownItem.PartCode,
                    QtyPacks = packs,
                    LFNeeded = crownLF,
                    TotalLFProvided = packs * crownItem.LFPerPack
                });
            }
        }

        // Ceilings
        foreach (var room in input.Rooms.Where(r => r.HasCeiling))
        {
            double ftPerPanel = PanelWidthFt(room.PanelWidthInches); // 1.0 or 1.5 depending on width
            int qty;
            double panelLen;

            bool widthwise = room.CeilingOrientation == CeilingOrientation.Widthwise;
            double width = room.WidthFt;
            double length = room.LengthFt;

            if (widthwise && width <= 25)
            {
                // Widthwise orientation with width <= 25 ft
                // Determine shipping length: choose user-specified if valid, else compute
                double desired = room.CeilingPanelLengthFt;
                double proposed;
                if (width > 20)
                {
                    // custom length equal to room width rounded up
                    proposed = Math.Ceiling(width);
                }
                else
                {
                    double w = Math.Ceiling(width);
                    if (w < 10) w = 10;
                    if (w > 20) w = 20;
                    if (((int)w) % 2 != 0) w += 1;
                    proposed = w;
                }
                // Use desired if valid (>= width, even, between 10 and 20 ft); otherwise use proposed
                if (desired >= width && desired <= 20 && desired >= 10 && ((int)desired % 2) == 0)
                {
                    panelLen = desired;
                }
                else
                {
                    panelLen = proposed;
                }
                // Single row; panels per row determined by building length / panel width
                int panelsPerRow = (int)Math.Ceiling(length / ftPerPanel);
                qty = (int)Math.Ceiling(panelsPerRow * (1 + input.Options.Contingency));
            }
            else
            {
                // Lengthwise orientation or width > 25 ft: use original logic
                var perRow = Math.Ceiling(width / ftPerPanel);
                var rows = Math.Ceiling(length / room.CeilingPanelLengthFt);
                qty = (int)Math.Ceiling(perRow * rows * (1 + input.Options.Contingency));
                panelLen = room.CeilingPanelLengthFt;
            }
            if (result.CeilingPanels.ContainsKey(panelLen))
                result.CeilingPanels[panelLen] += qty;
            else
                result.CeilingPanels[panelLen] = qty;

            topTrackLF += 2 * (room.LengthFt + room.WidthFt);
        }
        if (topTrackLF > 0)
        {
            topTrackLF *= 1.05; // add 5%
            var trackItem = CatalogService.FindItem(catalog, "Top Track", 16, input.Options.Color);
            if (trackItem != null)
            {
                int packs = (int)Math.Ceiling(topTrackLF / trackItem.LFPerPack);
                result.Parts.Add(new PartRequirement
                {
                    PartCode = trackItem.PartCode,
                    QtyPacks = packs,
                    LFNeeded = topTrackLF,
                    TotalLFProvided = packs * trackItem.LFPerPack
                });
            }
        }

        // Hardware calculations
        int totalPanels = result.WallPanels.Values.Sum() + result.CeilingPanels.Values.Sum();
        result.Hardware.PlugSpacerPacks = totalPanels <= 0 ? 0 : Math.Max(1, (int)Math.Ceiling((totalPanels - 100) / 50.0));
        result.Hardware.ExpansionTools = totalPanels <= 250 ? 1 : 2;

        double wallPanelLenTotal = result.WallPanels.Sum(kvp => kvp.Key * kvp.Value);
        double ceilingPanelLenTotal = result.CeilingPanels.Sum(kvp => kvp.Key * kvp.Value);
        double wallTrimLF = jtrimLF + cornerLF + baseLF + crownLF;
        double ceilingTrimLF = topTrackLF;
        double wallScrews = (wallPanelLenTotal + wallTrimLF) / 2.0;
        double ceilingScrews = (ceilingPanelLenTotal + ceilingTrimLF) / 1.5;
        result.Hardware.WallScrewBoxes = (int)Math.Ceiling(wallScrews / 500.0);
        result.Hardware.CeilingScrewBoxes = (int)Math.Ceiling(ceilingScrews / 500.0);
        result.Hardware.ScrewBoxes = result.Hardware.WallScrewBoxes + result.Hardware.CeilingScrewBoxes;

        return result;
    }
}