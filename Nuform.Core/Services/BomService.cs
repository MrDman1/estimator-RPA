using System;
using System.Collections.Generic;
using Nuform.Core.Domain;

namespace Nuform.Core.Services;

public static class BomService
{
    public static IReadOnlyList<BomLineItem> Build(BuildingInput input, CalcEstimateResult result, CatalogService catalog, out bool missing)
    {
        var list = new List<BomLineItem>();
        missing = false;

        void Add(PartSpec spec, decimal qty, string? cat = null)
        {
            list.Add(new BomLineItem
            {
                PartNumber = spec.PartNumber,
                Name = spec.Description,
                Quantity = qty,
                Unit = spec.Units,
                Category = cat ?? spec.Category
            });
        }

        decimal wallPanelLf = 0m;
        decimal ceilingPanelLf = 0m;

        // Wall panels using resolver
        try
        {
            var color = PanelCodeResolver.ParseColor(input.WallPanelColor);
            var (code, name) = PanelCodeResolver.PanelSku(input.WallPanelWidthInches, (int)input.WallPanelLengthFt, color);
            var spec = new PartSpec
            {
                PartNumber = code,
                Description = name,
                Units = "PCS",
                LengthFt = (int)input.WallPanelLengthFt,
                Category = "Panels"
            };
            Add(spec, result.Panels.RoundedPanels, "Panels");
            wallPanelLf = result.Panels.RoundedPanels * (decimal)spec.LengthFt;
        }
        catch (Exception)
        {
            missing = true;
            Console.Error.WriteLine("Missing panel specification");
        }

        // Ceiling panels
        decimal roundedCeiling = 0m;
        if (input.IncludeCeilingPanels)
        {
            decimal panelWidthFt = input.CeilingPanelWidthInches == 18 ? 1.5m : 1.0m;
            int panelsNeeded;
            decimal panelLengthUsed;
            if (input.CeilingOrientation == CeilingOrientation.Widthwise)
            {
                int rows = (int)Math.Ceiling((decimal)input.Length / panelWidthFt);
                panelsNeeded = rows;
                panelLengthUsed = (decimal)input.Width;
            }
            else
            {
                int rows = (int)Math.Ceiling((decimal)input.Width / panelWidthFt);
                int perRow = (int)Math.Ceiling((decimal)input.Length / input.CeilingPanelLengthFt);
                panelsNeeded = rows * perRow;
                panelLengthUsed = input.CeilingPanelLengthFt;
            }
            var extraPercent = (decimal)(input.ExtraPercent ?? CalcSettings.DefaultExtraPercent);
            var withExtra = panelsNeeded * (1m + extraPercent / 100m);
            int baseCeiling = (int)Math.Ceiling(withExtra);
            roundedCeiling = CalcService.RoundPanels(baseCeiling);

            try
            {
                var color = PanelCodeResolver.ParseColor(input.CeilingPanelColor);
                var (code, name) = PanelCodeResolver.PanelSku(input.CeilingPanelWidthInches, (int)panelLengthUsed, color);
                var spec = new PartSpec
                {
                    PartNumber = code,
                    Description = name,
                    Units = "PCS",
                    LengthFt = (int)panelLengthUsed,
                    Category = "Panels"
                };
                Add(spec, roundedCeiling, "Panels");
                ceilingPanelLf = roundedCeiling * (decimal)spec.LengthFt;
            }
            catch (Exception)
            {
                missing = true;
                Console.Error.WriteLine("Missing ceiling panel specification");
            }
        }

        // Trim LF aggregation
        var wallColor = PanelCodeResolver.ParseColor(input.WallPanelColor);
        var ceilingColor = PanelCodeResolver.ParseColor(input.CeilingPanelColor);
        var wallAnyPanelOver12 = (double)input.WallPanelLengthFt > 12;
        var ceilingAnyPanelOver12 = (double)input.CeilingPanelLengthFt > 12;

        var wallTrimLF = new Dictionary<(TrimKind, NuformColor), double>();
        var ceilingTrimLF = new Dictionary<(TrimKind, NuformColor), double>();

        double wallPerimeter = input.Mode == "ROOM" ? 2 * (input.Length + input.Width) : input.Length;
        double openingsButtPerimeter = 0;
        double openingsWrappedPerimeter = 0;
        foreach (var op in input.Openings)
        {
            var per = 2 * (op.Width + op.Height) * op.Count;
            if (op.Treatment == OpeningTreatment.WRAPPED) openingsWrappedPerimeter += per;
            else openingsButtPerimeter += per;
        }

        if (input.Trims.JTrimEnabled)
        {
            AddLF(wallTrimLF, (TrimKind.J, wallColor), wallPerimeter);
            AddLF(wallTrimLF, (TrimKind.J, wallColor), openingsButtPerimeter);
            AddLF(wallTrimLF, (TrimKind.J, wallColor), openingsWrappedPerimeter);
            AddLF(wallTrimLF, (TrimKind.OutsideCorner, wallColor), openingsWrappedPerimeter);
        }

        var insideCorners = CalcService.ComputeInsideCorners(input);
        if (insideCorners > 0)
            AddLF(wallTrimLF, (TrimKind.InsideCorner, wallColor), insideCorners * input.Height);

        if (result.Trims.CeilingTransition != null)
        {
            var lf = wallPerimeter;
            switch (result.Trims.CeilingTransition)
            {
                case "cove":
                    AddLF(ceilingTrimLF, (TrimKind.Cove, ceilingColor), lf);
                    break;
                case "crown-base":
                    AddLF(ceilingTrimLF, (TrimKind.CrownBaseBase, ceilingColor), lf);
                    AddLF(ceilingTrimLF, (TrimKind.CrownBaseCap, ceilingColor), lf);
                    break;
                case "f-trim":
                    AddLF(ceilingTrimLF, (TrimKind.Transition, ceilingColor), lf);
                    break;
            }
        }

        foreach (var kv in wallTrimLF)
        {
            var kind = kv.Key.Item1; var color = kv.Key.Item2; var lf = kv.Value;
            var lenFt = TrimPolicy.DecideTrimLengthFeet(kind, wallAnyPanelOver12, lf, _ => TrimPolicy.PiecesPerPackage[kind]);
            var colorName = PanelCodeResolver.ColorName(color);
            var spec = catalog.FindByCategoryAndLength(colorName, CategoryFor(kind), lenFt);
            if (spec != null && Math.Abs(spec.LengthFt - lenFt) > 0.01)
                spec = null;
            if (spec == null)
            {
                spec = catalog.FindByCategoryAndLength("BRIGHT WHITE", CategoryFor(kind), lenFt);
                if (spec != null && Math.Abs(spec.LengthFt - lenFt) > 0.01)
                    spec = null;
            }
            if (spec == null)
            {
                missing = true;
                Console.Error.WriteLine($"Missing {kind} specification");
            }
            else
            {
                var packs = Math.Ceiling(lf / (spec.PackPieces * lenFt));
                Add(spec, (decimal)packs);
            }
        }

        foreach (var kv in ceilingTrimLF)
        {
            var kind = kv.Key.Item1; var color = kv.Key.Item2; var lf = kv.Value;
            var lenFt = TrimPolicy.DecideTrimLengthFeet(kind, ceilingAnyPanelOver12, lf, _ => TrimPolicy.PiecesPerPackage[kind]);
            var colorName = PanelCodeResolver.ColorName(color);
            var spec = catalog.FindByCategoryAndLength(colorName, CategoryFor(kind), lenFt);
            if (spec != null && Math.Abs(spec.LengthFt - lenFt) > 0.01)
                spec = null;
            if (spec == null)
            {
                spec = catalog.FindByCategoryAndLength("BRIGHT WHITE", CategoryFor(kind), lenFt);
                if (spec != null && Math.Abs(spec.LengthFt - lenFt) > 0.01)
                    spec = null;
            }
            if (spec == null)
            {
                missing = true;
                Console.Error.WriteLine($"Missing {kind} specification");
            }
            else
            {
                var packs = Math.Ceiling(lf / (spec.PackPieces * lenFt));
                Add(spec, (decimal)packs);
            }
        }

        // Hardware
        decimal trimLfTotal = 0m;
        foreach (var kv in wallTrimLF) trimLfTotal += (decimal)kv.Value;
        foreach (var kv in ceilingTrimLF) trimLfTotal += (decimal)kv.Value;

        if (input.IncludeWallScrews)
        {
            var pkgs = CalcScrewPackages(wallPanelLf, trimLfTotal, 2.0);
            if (pkgs > 0)
                Add(catalog.GetHardware("HPR016AANA"), pkgs, "Screws");
        }

        if (input.IncludeCeilingScrews)
        {
            var pkgs = CalcScrewPackages(ceilingPanelLf, trimLfTotal, 1.5);
            if (pkgs > 0)
                Add(catalog.GetHardware("HPR017AANA"), pkgs, "Screws");
        }

        if (input.IncludePlugs)
        {
            var plugCode = input.WallPanelColor.ToUpperInvariant() switch
            {
                "BLACK" => "GEL1PPAABK",
                "BRIGHT WHITE" => "GEL1PPAABW",
                _ => "GEL1PPAAWH"
            };
            Add(catalog.GetHardware(plugCode), 1, "Accessories");
        }

        if (input.IncludeSpacers)
            Add(catalog.GetHardware("GEL1PSADWH"), 1, "Accessories");

        if (input.IncludeExpansionTool)
            Add(catalog.GetHardware("HPR018AENA"), 1, "Accessories");

        return list;
    }

    private static void AddLF(Dictionary<(TrimKind, NuformColor), double> map, (TrimKind, NuformColor) key, double lf)
    {
        if (lf <= 0) return;
        map.TryGetValue(key, out var cur);
        map[key] = cur + lf;
    }

    private static string CategoryFor(TrimKind kind) => kind switch
    {
        TrimKind.J => "J",
        TrimKind.InsideCorner => "CornerInside",
        TrimKind.OutsideCorner => "CornerOutside",
        TrimKind.Transition => "F",
        TrimKind.DripEdge => "DripEdge",
        TrimKind.Cove => "Cove",
        TrimKind.CrownBaseBase => "CrownBaseBase",
        TrimKind.CrownBaseCap => "CrownBaseCap",
        TrimKind.H => "H",
        TrimKind.F => "F",
        _ => "J"
    };

    public static int CalcScrewPackages(decimal panelLf, decimal trimLf, double divisor)
    {
        var pieces = (double)(panelLf + trimLf) / divisor;
        return (int)Math.Ceiling(pieces / 500.0);
    }
}
