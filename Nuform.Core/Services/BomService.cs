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

        void Add(PartSpec spec, decimal qty)
        {
            list.Add(new BomLineItem
            {
                PartNumber = spec.PartNumber,
                Name = spec.Description,
                Quantity = qty,
                Unit = spec.Units,
                Category = spec.Category
            });
        }

        // ----- WALL PANELS -----
        try
        {
            var wallPanelSpec = catalog.ResolvePanelSku(
                input.WallPanelSeries,
                input.WallPanelWidthInches,
                input.WallPanelLengthFt,
                input.WallPanelColor);

            list.Add(new BomLineItem
            {
                PartNumber = wallPanelSpec.PartNumber,
                Name = wallPanelSpec.Description,
                Quantity = result.Panels.RoundedPanels,
                Unit = "pcs",
                Category = "Panel"
            });
        }
        catch (InvalidOperationException)
        {
            missing = true;
            Console.Error.WriteLine("Missing panel specification");
        }

        // ----- CEILING PANELS -----
        if (input.IncludeCeilingPanels)
        {
            decimal panelArea = (input.CeilingPanelWidthInches / 12m) * input.CeilingPanelLengthFt;
            decimal ceilingArea = (decimal)input.Length * (decimal)input.Width;
            var baseCeilingPanels = (int)Math.Ceiling(ceilingArea / panelArea);
            var extraPercent = (decimal)(input.ExtraPercent ?? CalcSettings.DefaultExtraPercent);
            var withExtra = baseCeilingPanels * (1m + extraPercent / 100m);
            var roundedCeiling = CalcService.RoundPanels((double)withExtra);

            try
            {
                var ceilSpec = catalog.ResolvePanelSku(
                    input.CeilingPanelSeries,
                    input.CeilingPanelWidthInches,
                    input.CeilingPanelLengthFt,
                    input.CeilingPanelColor);

                list.Add(new BomLineItem
                {
                    PartNumber = ceilSpec.PartNumber,
                    Name = $"{ceilSpec.Description} (CEILING)",
                    Quantity = roundedCeiling,
                    Unit = "pcs",
                    Category = "Panel"
                });
            }
            catch (InvalidOperationException)
            {
                missing = true;
                Console.Error.WriteLine("Missing ceiling panel specification");
            }
        }

        // J Trim
        if (input.Trims.JTrimEnabled && result.Trims.JTrimLF > 0)
        {
            var j = catalog.FindByCategoryAndLength("BRIGHT WHITE", "J", 12);
            if (j == null)
            {
                missing = true;
                Console.Error.WriteLine("Missing J Trim specification");
            }
            else
            {
                var packs = Math.Ceiling(result.Trims.JTrimLF / (j.PackPieces * j.LengthFt));
                Add(j, (decimal)packs);
            }
        }

        // Inside Corners
        if (result.InsideCorners > 0)
        {
            var ic = catalog.FindByCategoryAndLength("BRIGHT WHITE", "CornerInside", 12);
            if (ic == null)
            {
                missing = true;
                Console.Error.WriteLine("Missing Inside Corner specification");
            }
            else
            {
                var lf = result.InsideCorners * input.Height;
                var packs = Math.Ceiling(lf / (ic.PackPieces * ic.LengthFt));
                Add(ic, (decimal)packs);
            }
        }

        // Ceiling Transition trims
        if (result.Trims.CeilingTransition != null && result.Trims.CeilingTrimLF > 0)
        {
            switch (result.Trims.CeilingTransition)
            {
                case "cove":
                    var cv = catalog.FindByCategoryAndLength("BRIGHT WHITE", "Cove", 12);
                    if (cv == null)
                    {
                        missing = true;
                        Console.Error.WriteLine("Missing Cove Trim specification");
                    }
                    else
                    {
                        var packs = Math.Ceiling(result.Trims.CeilingTrimLF / (cv.PackPieces * cv.LengthFt));
                        Add(cv, (decimal)packs);
                    }
                    break;
                case "crown-base":
                    var baseSpec = catalog.FindByCategoryAndLength("NUFORM WHITE", "CrownBaseBase", 16);
                    var capSpec = catalog.FindByCategoryAndLength("NUFORM WHITE", "CrownBaseCap", 16);
                    if (baseSpec == null || capSpec == null)
                    {
                        missing = true;
                        if (baseSpec == null) Console.Error.WriteLine("Missing Crown/Base base specification");
                        if (capSpec == null) Console.Error.WriteLine("Missing Crown/Base cap specification");
                    }
                    else
                    {
                        var packs = Math.Ceiling(result.Trims.CeilingTrimLF / (baseSpec.PackPieces * baseSpec.LengthFt));
                        Add(baseSpec, (decimal)packs);
                        Add(capSpec, (decimal)packs);
                    }
                    break;
            }
        }

        return list;
    }
}
