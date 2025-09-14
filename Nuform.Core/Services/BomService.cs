using System;
using System.Collections.Generic;
using Nuform.Core.Domain;

namespace Nuform.Core.Services;

// Patched version of BomService.cs with corrected J‑Trim logic.  When
// ceiling panels are included and no ceiling transition trim is selected,
// two extra runs of J‑Trim are added for the top and ceiling tracks.
public static class BomService
{
    public static IReadOnlyList<BomLineItem> Build(BuildingInput input, CalcEstimateResult result, CatalogService catalog, out bool missing)
    {
        var list = new List<BomLineItem>();
        missing = false;

        // Add a BOM line item, including overage (linear footage or quantity difference) when available.
        void Add(PartSpec spec, decimal qty, string? cat = null, decimal overage = 0m)
        {
            list.Add(new BomLineItem
            {
                PartNumber = spec.PartNumber,
                Name = spec.Description,
                Quantity = qty,
                Unit = spec.Units,
                Category = cat ?? spec.Category,
                Overage = overage
            });
        }

        decimal wallPanelLf = 0m;
        // Trim LF maps and ceiling locals declared early so they exist before use
        var wallTrimLF = new Dictionary<(TrimKind, NuformColor), double>();
        var ceilingTrimLF = new Dictionary<(TrimKind, NuformColor), double>();
        double pendingCeilingHlf = 0.0;
        int chosenCeilShipLen = 0;
        int roundedCeiling = 0;
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
            Add(spec, result.Panels.RoundedPanels, "Panels", 0m);
            wallPanelLf = result.Panels.RoundedPanels * (decimal)spec.LengthFt;
        }
        catch (Exception)
        {
            missing = true;
            Console.Error.WriteLine("Missing panel specification");
        }

        // Ceiling panels (auto length + orientation; store H-Trim LF to add later)
        if (input.IncludeCeilingPanels)
        {
            // Determine panel width in feet: 12" => 1.0, 18" => 1.5.
            decimal ftPerPanel = input.CeilingPanelWidthInches == 18 ? 1.5m : 1.0m;

            // Round up to the nearest standard shipping length between 10–20 ft (even).  Used for
            // lengthwise orientation and widthwise when width <= 20.
            int RoundUpStd(decimal ft)
            {
                int v = (int)Math.Ceiling(ft);
                if (v < 10) v = 10;
                if (v > 20) v = 20;
                // Ensure even (panels come in 2-ft increments)
                if (v % 2 == 1) v += 1;
                if (v == 11) v = 12;
                if (v == 13) v = 14;
                if (v == 15) v = 16;
                if (v == 17) v = 18;
                return v;
            }

            // Determine the building dimensions as decimals.
            decimal buildingLength = (decimal)input.Length;
            decimal buildingWidth = (decimal)input.Width;

            // Decide if the ceiling should run along the length (lengthwise) or width (widthwise).
            bool isLengthwise = input.CeilingOrientation == CeilingOrientation.Lengthwise;

            // Placeholder for computed row/column counts and shipping length.
            int rows;
            int panelsPerRow;
            int shipLen;

            // Widthwise ceilings: run panels along the width dimension.  If the width exceeds 25 ft,
            // revert to the lengthwise algorithm.  Otherwise choose a shipping length based on the
            // width; there is always only one row.
            if (!isLengthwise)
            {
                // If building width > 25 ft, treat orientation as lengthwise and fall back to
                // the same algorithm used for lengthwise ceilings (multiple rows and shipping
                // lengths based on the length dimension).  In this case, we simply swap the
                // dimensions and set isLengthwise = true below.
                if (buildingWidth > 25m)
                {
                    isLengthwise = true;
                }
                else
                {
                    // For widths up to 25 ft, choose the shipping length.  If width > 20 ft,
                    // use a custom length equal to the ceiling width rounded up to the next
                    // whole foot (this avoids splitting into multiple rows).  Otherwise, use
                    // the nearest even standard length >= width.
                    if (buildingWidth > 20m)
                    {
                        shipLen = (int)Math.Ceiling(buildingWidth);
                    }
                    else
                    {
                        shipLen = RoundUpStd(buildingWidth);
                    }

                    // Only one row when widthwise: panels run across the building width.  The number
                    // of panels per row is based on the building length divided by the panel width.
                    rows = 1;
                    panelsPerRow = (int)Math.Ceiling(buildingLength / ftPerPanel);

                    // Calculate total panels and apply the extra percentage.
                    int totalPanels = panelsPerRow * rows;
                    var extraPercent = (decimal)(input.ExtraPercent ?? CalcSettings.DefaultExtraPercent);
                    var withExtra = totalPanels * (1m + extraPercent / 100m);
                    int baseCeiling = (int)Math.Ceiling(withExtra);
                    roundedCeiling = CalcService.RoundPanels(baseCeiling);
                    chosenCeilShipLen = shipLen;

                    try
                    {
                        var color = PanelCodeResolver.ParseColor(input.CeilingPanelColor);
                        var (code, name) = PanelCodeResolver.PanelSku(input.CeilingPanelWidthInches, shipLen, color);
                        var spec = new PartSpec
                        {
                            PartNumber = code,
                            Description = name,
                            Units = "PCS",
                            LengthFt = shipLen,
                            Category = "Panels"
                        };
                        Add(spec, roundedCeiling, "Panels");
                        ceilingPanelLf = roundedCeiling * (decimal)spec.LengthFt;
                    }
                    catch
                    {
                        missing = true;
                        Console.Error.WriteLine("Missing ceiling panel specification");
                    }

                    // Widthwise orientation uses a single row, so no H‑trim is required between rows.
                    pendingCeilingHlf = 0.0;
                }
            }

            // Lengthwise algorithm (or reverted widthwise when width > 25 ft): choose rows and
            // shipping length to minimize waste using standard 10–20 ft even lengths.  This
            // algorithm replicates the original logic but avoids swapping L/W because the
            // dimensions are used directly.
            if (isLengthwise)
            {
                // For lengthwise orientation, panels run along the length dimension.  The
                // number of panels per row is based on the building width divided by the
                // panel width.
                panelsPerRow = (int)Math.Ceiling(buildingWidth / ftPerPanel);

                // Optional manual override (CeilingPanelLengthFt).  If provided and valid (even
                // between 10 and 20 ft), use it; otherwise compute rows and shipping length
                // automatically.
                int? userLen = null;
                try
                {
                    var t = input.GetType();
                    var p = t.GetProperty("CeilingPanelLengthFt") ?? t.GetProperty("CeilingLen") ?? t.GetProperty("CeilingLengthFt");
                    if (p != null)
                    {
                        var val = p.GetValue(input);
                        if (val is int iv) userLen = iv;
                        else if (val is double dv) userLen = (int)Math.Round(dv);
                        else if (val is decimal mv) userLen = (int)Math.Ceiling(mv);
                    }
                }
                catch { /* ignore manual override and fall back to computed */ }

                if (userLen.HasValue && userLen.Value >= 10 && userLen.Value <= 20 && userLen.Value % 2 == 0)
                {
                    shipLen = userLen.Value;
                    rows = (int)Math.Ceiling(buildingLength / shipLen);
                }
                else
                {
                    // Choose rows to minimize waste across standard lengths.  A higher number of
                    // rows implies shorter shipping lengths.
                    int minRows = (int)Math.Ceiling(buildingLength / 20m);
                    int maxRows = (int)Math.Ceiling(buildingLength / 10m);
                    if (maxRows < minRows) maxRows = minRows;

                    int bestRows = minRows;
                    int bestShip = 20;
                    decimal bestWaste = decimal.MaxValue;
                    for (int r = minRows; r <= maxRows; r++)
                    {
                        int s = RoundUpStd(buildingLength / r);
                        decimal waste = r * s - buildingLength;
                        if (waste < bestWaste || (Math.Abs(waste - bestWaste) < 0.0001m && s > bestShip))
                        {
                            bestWaste = waste;
                            bestShip = s;
                            bestRows = r;
                        }
                    }
                    rows = bestRows;
                    shipLen = bestShip;
                }

                int totalPanels = panelsPerRow * rows;
                var extraPercent = (decimal)(input.ExtraPercent ?? CalcSettings.DefaultExtraPercent);
                var withExtra = totalPanels * (1m + extraPercent / 100m);
                int baseCeiling = (int)Math.Ceiling(withExtra);
                roundedCeiling = CalcService.RoundPanels(baseCeiling);
                chosenCeilShipLen = shipLen;

                try
                {
                    var color = PanelCodeResolver.ParseColor(input.CeilingPanelColor);
                    var (code, name) = PanelCodeResolver.PanelSku(input.CeilingPanelWidthInches, shipLen, color);
                    var spec = new PartSpec
                    {
                        PartNumber = code,
                        Description = name,
                        Units = "PCS",
                        LengthFt = shipLen,
                        Category = "Panels"
                    };
                    Add(spec, roundedCeiling, "Panels");
                    ceilingPanelLf = roundedCeiling * (decimal)spec.LengthFt;
                }
                catch
                {
                    missing = true;
                    Console.Error.WriteLine("Missing ceiling panel specification");
                }

                // H‑trim runs along the perpendicular span between rows.  Only add H‑trim if there
                // is more than one row.
                decimal hTrimLF = Math.Max(0, rows - 1) * buildingWidth;
                pendingCeilingHlf = rows > 1 ? (double)hTrimLF : 0.0;
            }
        }

        // Trim LF aggregation
        var wallColor = PanelCodeResolver.ParseColor(input.WallPanelColor);
        var ceilingColor = PanelCodeResolver.ParseColor(input.CeilingPanelColor);
        var wallAnyPanelOver12 = (double)input.WallPanelLengthFt > 12;
        var ceilingAnyPanelOver12 = chosenCeilShipLen > 12;

        // Add ceiling H-Trim (if any) now that we have the LF dictionaries.
        if (pendingCeilingHlf > 0.0)
        {
            AddLF(ceilingTrimLF, (TrimKind.H, ceilingColor), pendingCeilingHlf);
        }

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
            // Compute total J-Trim LF for bottom track and openings
            double jlf = wallPerimeter + openingsButtPerimeter + openingsWrappedPerimeter;
            // Add the base J-Trim requirement (bottom track)
            AddLF(wallTrimLF, (TrimKind.J, wallColor), jlf);
            // If a ceiling is present and no transition trim is selected, J-Trim must cover
            // both the top track and the ceiling track.  Add two extra runs.
            if (input.IncludeCeilingPanels && result.Trims.CeilingTransition == null)
            {
                AddLF(wallTrimLF, (TrimKind.J, wallColor), jlf * 2.0);
            }
            // Outside corners still require their own trim kind
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
                // Compute overage in linear feet: provided LF minus required LF
                var providedLf = packs * (spec.PackPieces * lenFt);
                var over = (decimal)providedLf - (decimal)lf;
                Add(spec, (decimal)packs, null, over);
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
                var providedLf = packs * (spec.PackPieces * lenFt);
                var over = (decimal)providedLf - (decimal)lf;
                Add(spec, (decimal)packs, null, over);
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
            {
                // Overages for screws measured in pieces: each box contains 500 pieces.
                var piecesRequired = (decimal)((double)(wallPanelLf + trimLfTotal) / 2.0);
                var piecesProvided = (decimal)pkgs * 500m;
                var overScrews = piecesProvided - piecesRequired;
                Add(catalog.GetHardware("HPR016AANA"), pkgs, "Screws", overScrews);
            }
        }

        if (input.IncludeCeilingScrews)
        {
            var pkgs = CalcScrewPackages(ceilingPanelLf, trimLfTotal, 1.5);
            if (pkgs > 0)
            {
                var piecesRequired = (decimal)((double)(ceilingPanelLf + trimLfTotal) / 1.5);
                var piecesProvided = (decimal)pkgs * 500m;
                var overScrews = piecesProvided - piecesRequired;
                Add(catalog.GetHardware("HPR017AANA"), pkgs, "Screws", overScrews);
            }
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
        TrimKind.Transition => "J",
        TrimKind.DripEdge => "DripEdge",
        TrimKind.Cove => "Cove",
        TrimKind.CrownBaseBase => "CrownBaseBase",
        TrimKind.CrownBaseCap => "CrownBaseCap",
        TrimKind.H => "H",
        TrimKind.F => "J",
        _ => "J"
    };

    public static int CalcScrewPackages(decimal panelLf, decimal trimLf, double divisor)
    {
        var pieces = (double)(panelLf + trimLf) / divisor;
        return (int)Math.Ceiling(pieces / 500.0);
    }
}