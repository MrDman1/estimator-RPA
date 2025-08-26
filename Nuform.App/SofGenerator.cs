using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Nuform.App.Services;

using Nuform.Core;
using Nuform.Core.Domain;
using Nuform.Core.Services;

// ===== Use the LegacyCompat shapes that SOF export expects =====
using AppConfig = Nuform.Core.LegacyCompat.AppConfig;
using EstimateResult = Nuform.Core.LegacyCompat.EstimateResult;
using Room = Nuform.Core.LegacyCompat.Room;
using CatalogItem = Nuform.Core.LegacyCompat.CatalogItem;
using FileNaming = Nuform.Core.LegacyCompat.FileNaming;

namespace Nuform.App;

/// <summary>
/// Generates Nuform Component Order .SOF files from an <see cref="EstimateResult"/>.
/// </summary>
public static class SofGenerator
{
    /// <summary>
    /// Builds a .SOF text file for the supplied estimate and returns the
    /// path to the generated file. The output is saved to the BOM's
    /// <c>1-CURRENT</c> directory.
    /// </summary>
    /// <param name="cfg">Application configuration.</param>
    /// <param name="bomNumber">The BOM number associated with the estimate.</param>
    /// <param name="result">The estimate result.</param>
    /// <returns>The path to the generated .SOF file.</returns>
    private static void EnsureDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Output path is empty.");
        Directory.CreateDirectory(path);
    }

    public static string Generate(AppConfig cfg, string bomNumber, EstimateResult result)
    {
        // Determine the BOM folder and ensure it exists and is writable.
        var bomFolder = Path.Combine(cfg.WipDesignRoot, bomNumber, "1-CURRENT");
        EnsureDirectory(bomFolder);
        var probe = Path.Combine(bomFolder, ".write_probe");
        using (File.Create(probe, 1, FileOptions.DeleteOnClose)) { }

        // Load catalog information so that unit pricing can be looked up.
        // The catalog path mirrors the default used by the estimator when
        // calculating quantities.
        var catalog = Nuform.Core.LegacyCompat.CatalogService.Load("RELINE Part List 2025-1.0.pdf");
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

        var sofPath = Path.Combine(bomFolder, bomNumber + ".sof");
        File.WriteAllLines(sofPath, lines);
        return sofPath;
    }
}
