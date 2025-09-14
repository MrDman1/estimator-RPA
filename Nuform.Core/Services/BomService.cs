using Nuform.Core.Domain;
using Nuform.Core.LegacyCompat; // keep if other types in this file require it
using System;
using System.Collections.Generic;

namespace Nuform.Core.Services
{
    // Patched BomService:
    //  - Build signature uses CalcEstimateResult (matches caller)
    //  - Ceiling trims use ceiling color and have robust color fallbacks
    //  - J-Trim extra runs when no ceiling transition is selected (as before)
    //  - Overage assignment is non-nullable-safe
    public static class BomService
    {
        public static IReadOnlyList<BomLineItem> Build(
            BuildingInput input,
            CalcEstimateResult result,     // <— FIX: use CalcEstimateResult (not Legacy EstimateResult)
            CatalogService catalog,
            out bool missing)
        {
            var list = new List<BomLineItem>();
            missing = false;

            void Add(PartSpec spec, decimal qty, string? cat = null, decimal? overage = null)
            {
                list.Add(new BomLineItem
                {
                    PartNumber = spec.PartNumber,
                    Name = spec.Description,
                    Quantity = qty,
                    Unit = spec.Units,
                    Category = cat ?? spec.Category,
                    Overage = overage ?? 0m   // <— FIX: model property is decimal (non-nullable)
                });
            }

            decimal wallPanelLf = 0m;

            // Trim LF buckets
            var wallTrimLF = new Dictionary<(TrimKind, NuformColor), double>();
            var ceilingTrimLF = new Dictionary<(TrimKind, NuformColor), double>();
            double pendingCeilingHlf = 0.0;

            static void AddLF(Dictionary<(TrimKind, NuformColor), double> map, (TrimKind, NuformColor) key, double lf)
            {
                if (lf <= 0) return;
                if (!map.TryGetValue(key, out var cur)) cur = 0;
                map[key] = cur + lf;
            }

            // === Panels (your existing computation remains elsewhere) ===

            // Colors for trim selection
            var wallColor = PanelCodeResolver.ParseColor(input.WallPanelColor);
            var ceilingColor = PanelCodeResolver.ParseColor(input.CeilingPanelColor);

            // Decide stick length policy booleans (keep your real logic if you compute ship length)
            var wallAnyPanelOver12 = (double)input.WallPanelLengthFt > 12;
            var ceilingAnyPanelOver12 = true;

            // Add ceiling H-Trim (if any) when LF is known
            if (pendingCeilingHlf > 0.0)
            {
                AddLF(ceilingTrimLF, (TrimKind.H, ceilingColor), pendingCeilingHlf);
            }

            // Room perimeters / openings
            double wallPerimeter = input.Mode == "ROOM" ? 2 * (input.Length + input.Width) : input.Length;
            double openingsButtPerimeter = 0;
            double openingsWrappedPerimeter = 0;
            foreach (var op in input.Openings)
            {
                var per = 2 * (op.Width + op.Height) * op.Count;
                if (op.Treatment == OpeningTreatment.WRAPPED) openingsWrappedPerimeter += per;
                else openingsButtPerimeter += per;
            }

            // === WALL TRIMS (use wall color) ===
            if (input.Trims.JTrimEnabled)
            {
                // Bottom track + around openings
                double jlf = wallPerimeter + openingsButtPerimeter + openingsWrappedPerimeter;

                // Base J-Trim
                AddLF(wallTrimLF, (TrimKind.J, wallColor), jlf);

                // If ceiling panels and no transition trim → add two extra runs (top + ceiling track)
                if (input.IncludeCeilingPanels && input.Trims.CeilingTransition == null)  // <— FIX: use input.Trims
                {
                    AddLF(wallTrimLF, (TrimKind.J, wallColor), jlf * 2.0);
                }

                // Outside corners for wrapped openings
                AddLF(wallTrimLF, (TrimKind.OutsideCorner, wallColor), openingsWrappedPerimeter);
            }

            var insideCorners = CalcService.ComputeInsideCorners(input);
            if (insideCorners > 0)
                AddLF(wallTrimLF, (TrimKind.InsideCorner, wallColor), insideCorners * input.Height);

            // === CEILING TRANSITION TRIMS (use ceiling color) ===
            if (input.Trims.CeilingTransition != null)   // <— FIX: use input.Trims
            {
                var lf = wallPerimeter;
                switch (input.Trims.CeilingTransition)   // values like "cove", "crown-base", "f-trim"
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

            // === MATERIALIZE TRIM PACKS (WALL) ===
            foreach (var kv in wallTrimLF)
            {
                var kind = kv.Key.Item1;
                var color = kv.Key.Item2;
                var lf = kv.Value;

                var lenFt = TrimPolicy.DecideTrimLengthFeet(kind, wallAnyPanelOver12, lf, _ => TrimPolicy.PiecesPerPackage[kind]);
                var colorName = PanelCodeResolver.ColorName(color);

                var spec = FindSpecWithFallbacks(catalog, colorName, CategoryFor(kind), lenFt);
                if (spec == null)
                {
                    missing = true;
                    // Console.Error.WriteLine($"Missing {kind} spec for {colorName} @ {lenFt}ft");
                    continue;
                }

                var packs = Math.Ceiling(lf / (spec.PackPieces * lenFt));
                var providedLf = packs * (spec.PackPieces * lenFt);
                var overNative = (decimal)providedLf - (decimal)lf; // native LF overage
                Add(spec, (decimal)packs, null, overNative);
            }

            // === MATERIALIZE TRIM PACKS (CEILING) ===
            foreach (var kv in ceilingTrimLF)
            {
                var kind = kv.Key.Item1;
                var color = kv.Key.Item2;
                var lf = kv.Value;

                var lenFt = TrimPolicy.DecideTrimLengthFeet(kind, ceilingAnyPanelOver12, lf, _ => TrimPolicy.PiecesPerPackage[kind]);
                var colorName = PanelCodeResolver.ColorName(color);

                var spec = FindSpecWithFallbacks(catalog, colorName, CategoryFor(kind), lenFt);
                if (spec == null)
                {
                    missing = true;
                    // Console.Error.WriteLine($"Missing {kind} spec for {colorName} @ {lenFt}ft");
                    continue;
                }

                var packs = Math.Ceiling(lf / (spec.PackPieces * lenFt));
                var providedLf = packs * (spec.PackPieces * lenFt);
                var overNative = (decimal)providedLf - (decimal)lf; // native LF overage
                Add(spec, (decimal)packs, null, overNative);
            }

            // Screws & accessories — unchanged in this patch
            // (keep your existing code below if present)

            return list;
        }

        // Robust color/category/length resolver with fallbacks.
        // Order: preferred color → BRIGHT WHITE → NUFORM WHITE.
        private static PartSpec? FindSpecWithFallbacks(
            CatalogService catalog,
            string preferredColorName,
            string category,
            double lengthFt)
        {
            string[] colors =
            {
                preferredColorName,
                "BRIGHT WHITE",
                "NUFORM WHITE"
            };

            foreach (var color in colors)
            {
                var spec = catalog.FindByCategoryAndLength(color, category, lengthFt);
                if (spec == null) continue;

                // Keep your existing length guard
                if (Math.Abs(spec.LengthFt - lengthFt) > 0.01) continue;

                return spec;
            }

            return null;
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
            _ => "Trim"
        };
    }
}