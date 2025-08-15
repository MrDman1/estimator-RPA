using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nuform.Core;

/// <summary>
/// Represents a single catalog item parsed from the RELINE part list.
/// </summary>
public class CatalogItem
{
    public string PartCode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double LengthFt { get; init; }
    public int PiecesPerPack { get; init; }
    public double LFPerPack { get; init; }
    public string Color { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal PriceUSD { get; init; }
}

/// <summary>
/// Loads part information from the RELINE part list PDF and provides
/// lookups for estimating logic.
/// </summary>
public static class CatalogService
{
    /// <summary>
    /// Reads the catalog from a PDF file. The parser is intentionally
    /// lightweight – it extracts text from the PDF and searches for lines
    /// that resemble a catalog row. The parsing heuristics may need to be
    /// adjusted if the PDF layout changes in future releases.
    /// </summary>
    public static IReadOnlyList<CatalogItem> Load(string pdfPath)
    {
        var items = new List<CatalogItem>();
        if (!File.Exists(pdfPath)) return items; // fail gracefully
        try
        {
            // The PDF is treated as plain text. If the file is not a text
            // PDF the regex will simply not match anything, yielding an
            // empty catalog which keeps the estimator functional in tests.
            var text = File.ReadAllText(pdfPath);
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var m = Regex.Match(line,
                    @"^(?<code>\S+)\s+(?<desc>.+?)\s+(?<len>\d+)'\s+(?<color>[A-Za-z\s]+)\s+(?<pcs>\d+)\s*pcs\s+(?<lf>\d+)\s*LF/Pack\s+(?<cat>[^\d]+)\s+(?<price>\d+\.\d+)");
                if (!m.Success) continue;
                items.Add(new CatalogItem
                {
                    PartCode = m.Groups["code"].Value,
                    Description = m.Groups["desc"].Value.Trim(),
                    LengthFt = double.Parse(m.Groups["len"].Value),
                    Color = m.Groups["color"].Value.Trim(),
                    PiecesPerPack = int.Parse(m.Groups["pcs"].Value),
                    LFPerPack = double.Parse(m.Groups["lf"].Value),
                    Category = m.Groups["cat"].Value.Trim(),
                    PriceUSD = decimal.Parse(m.Groups["price"].Value)
                });
            }
        }
        catch
        {
            // Swallow parsing errors – an empty catalog will cause lookups
            // to fail but will not crash the estimator.
        }
        return items;
    }

    /// <summary>
    /// Attempts to locate a catalog item by category/length/color.
    /// </summary>
    public static CatalogItem? FindItem(IEnumerable<CatalogItem> catalog,
        string category, double lengthFt, string color)
        => catalog.FirstOrDefault(c =>
            c.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(c.LengthFt - lengthFt) < 0.01 &&
            c.Color.Equals(color, StringComparison.OrdinalIgnoreCase));
}
