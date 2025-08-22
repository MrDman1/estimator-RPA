using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Nuform.Core;
using Nuform.Core.LegacyCompat;

namespace Nuform.App;

/// <summary>
/// Generates Nuform Component Order .SOF files from an <see cref="EstimateResult"/>.
/// </summary>
public static class SofGenerator
{
    /// <summary>
    /// Builds a .SOF text file for the supplied estimate and returns the
    /// path to the generated file. The output is saved to the BOM's
    /// <c>1-CURRENT</c> directory if it can be located via
    /// <see cref="PathDiscovery"/>.
    /// </summary>
    /// <param name="cfg">Application configuration.</param>
    /// <param name="bomNumber">The BOM number associated with the estimate.</param>
    /// <param name="result">The estimate result.</param>
    /// <returns>The path to the generated .SOF file or <c>null</c> if the
    /// BOM folder could not be located.</returns>
    public static string? Generate(AppConfig cfg, string bomNumber, EstimateResult result)
    {
        // Discover the BOM folder. The estimator stores SOF files inside
        // the "1-CURRENT" sub directory for a BOM.
        var bomFolder = PathDiscovery.FindBomFolder(cfg.WipDesignRoot, bomNumber);
        if (bomFolder == null)
            return null;

        // Load catalog information so that unit pricing can be looked up.
        // The catalog path mirrors the default used by the estimator when
        // calculating quantities.
        var catalog = CatalogService.Load("RELINE Part List 2025-1-0.pdf");
        var lookup = catalog.ToDictionary(c => c.PartCode, StringComparer.OrdinalIgnoreCase);

        static decimal PriceFor(Dictionary<string, CatalogItem> cats, string partCode)
            => cats.TryGetValue(partCode, out var item) ? item.PriceUSD : 0m;

        var lines = new List<string>();
        foreach (var part in result.Parts)
        {
            var unit = PriceFor(lookup, part.PartCode);
            var qty = part.QtyPacks > 0 ? part.QtyPacks : (int)Math.Ceiling(part.LFNeeded);
            var ext = unit * qty;
            lines.Add(string.Join('\t', part.PartCode,
                qty.ToString(CultureInfo.InvariantCulture),
                unit.ToString("F2", CultureInfo.InvariantCulture),
                ext.ToString("F2", CultureInfo.InvariantCulture)));
        }

        // Basic hardware lines. In the absence of part codes in the
        // estimate result these are simple text entries and pricing will
        // be zero if the catalog does not contain a matching code.
        if (result.Hardware.PlugSpacerPacks > 0)
        {
            const string code = "PLUGSPACER";
            var unit = PriceFor(lookup, code);
            var ext = unit * result.Hardware.PlugSpacerPacks;
            lines.Add(string.Join('\t', code,
                result.Hardware.PlugSpacerPacks.ToString(CultureInfo.InvariantCulture),
                unit.ToString("F2", CultureInfo.InvariantCulture),
                ext.ToString("F2", CultureInfo.InvariantCulture)));
        }
        if (result.Hardware.ExpansionTools > 0)
        {
            const string code = "EXPANSIONTOOL";
            var unit = PriceFor(lookup, code);
            var ext = unit * result.Hardware.ExpansionTools;
            lines.Add(string.Join('\t', code,
                result.Hardware.ExpansionTools.ToString(CultureInfo.InvariantCulture),
                unit.ToString("F2", CultureInfo.InvariantCulture),
                ext.ToString("F2", CultureInfo.InvariantCulture)));
        }
        if (result.Hardware.ScrewBoxes > 0)
        {
            const string code = "SCREWBOX";
            var unit = PriceFor(lookup, code);
            var ext = unit * result.Hardware.ScrewBoxes;
            lines.Add(string.Join('\t', code,
                result.Hardware.ScrewBoxes.ToString(CultureInfo.InvariantCulture),
                unit.ToString("F2", CultureInfo.InvariantCulture),
                ext.ToString("F2", CultureInfo.InvariantCulture)));
        }

        Directory.CreateDirectory(bomFolder);
        var sofPath = Path.Combine(bomFolder, bomNumber + ".sof");
        File.WriteAllLines(sofPath, lines);
        return sofPath;
    }
}
