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
            decimal panelArea = (input.CeilingPanelWidthInches / 12m) * input.CeilingPanelLengthFt;
            decimal ceilingArea = (decimal)input.Length * (decimal)input.Width;
            var baseCeilingPanels = (int)Math.Ceiling(ceilingArea / panelArea);
            var extraPercent = (decimal)(input.ExtraPercent ?? CalcSettings.DefaultExtraPercent);
            var withExtra = baseCeilingPanels * (1m + extraPercent / 100m);
            roundedCeiling = CalcService.RoundPanels((double)withExtra);

            try
            {
                var color = PanelCodeResolver.ParseColor(input.CeilingPanelColor);
                var (code, name) = PanelCodeResolver.PanelSku(input.CeilingPanelWidthInches, (int)input.CeilingPanelLengthFt, color);
                var spec = new PartSpec
                {
                    PartNumber = code,
                    Description = name,
                    Units = "PCS",
                    LengthFt = (int)input.CeilingPanelLengthFt,
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

        // Inside corners
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

        // Ceiling transition trims
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

        // Hardware
        decimal trimLfTotal = (decimal)result.Trims.JTrimLF + (decimal)result.Trims.CeilingTrimLF + result.InsideCorners * (decimal)input.Height;

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

    public static int CalcScrewPackages(decimal panelLf, decimal trimLf, double divisor)
    {
        var pieces = (double)(panelLf + trimLf) / divisor;
        return (int)Math.Ceiling(pieces / 500.0);
    }
}
