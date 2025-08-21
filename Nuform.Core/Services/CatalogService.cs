using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Nuform.Core.Domain;

namespace Nuform.Core.Services;

public class CatalogService
{
    private readonly Dictionary<string, PartSpec> _parts;

    public CatalogService(string? csvPath = null)
    {
        csvPath ??= Path.Combine(AppContext.BaseDirectory, "Data", "parts.csv");
        _parts = Load(csvPath);
    }

    static Dictionary<string, PartSpec> Load(string path)
    {
        var dict = new Dictionary<string, PartSpec>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path)) return dict;
        foreach (var line in File.ReadAllLines(path).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',');
            if (cols.Length < 7) continue;
            dict[cols[0].Trim()] = new PartSpec
            {
                PartNumber = cols[0].Trim(),
                Description = cols[1].Trim(),
                Units = cols[2].Trim(),
                PackPieces = int.Parse(cols[3].Trim(), CultureInfo.InvariantCulture),
                LengthFt = double.Parse(cols[4].Trim(), CultureInfo.InvariantCulture),
                Color = cols[5].Trim(),
                Category = cols[6].Trim()
            };
        }
        return dict;
    }

    public IReadOnlyDictionary<string, PartSpec> GetAll() => _parts;

    public PartSpec? FindByCategoryAndLength(string color, string category, double lengthFt)
    {
        return _parts.Values
            .Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)
                        && p.Color.Equals(color, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => Math.Abs(p.LengthFt - lengthFt))
            .FirstOrDefault();
    }

    public PartSpec? FindPanel(string color, double widthInches, double lengthFt)
    {
        return _parts.Values
            .Where(p => p.Category.Equals("Panel", StringComparison.OrdinalIgnoreCase)
                        && p.Color.Equals(color, StringComparison.OrdinalIgnoreCase)
                        && Math.Abs(p.LengthFt - lengthFt) < 0.01
                        && p.Description.Contains(((int)widthInches).ToString(), StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
    }
}
