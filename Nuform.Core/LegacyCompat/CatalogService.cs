using System.Collections.Generic;
using System.Linq;
using Nuform.Core.Services;

namespace Nuform.Core.LegacyCompat;

public static class CatalogService
{
    public static IReadOnlyList<CatalogItem> Load(string? _)
    {
        var svc = new Nuform.Core.Services.CatalogService();
        return svc.GetAll().Values.Select(p => new CatalogItem
        {
            PartCode = p.PartNumber,
            Description = p.Description,
            Unit = p.Units,
            Category = p.Category,
            PackPieces = p.PackPieces,
            LengthFt = (decimal)p.LengthFt,
            Color = p.Color,
            PriceUSD = 0m
        }).ToList();
    }
}
