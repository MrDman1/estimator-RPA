using System.Linq;
using Nuform.Core.Domain;
using Nuform.Core.Services;
using Xunit;

namespace Nuform.Tests;

public class CatalogTests
{
    [Fact]
    public void CatalogContainsRelineSkus()
    {
        var catalog = new CatalogService();
        var all = catalog.GetAll();
        string[] required = new[] {
            "GEL1TJCBBW",
            "GEL1TJEBWH",
            "GERLCTCEBW",
            "TEMUTBEBWH",
            "TEMUTCEBWH",
            "GEL2SECEBW",
            "GEL1SECEBW",
            "GEL2PLCAWH",
            "GEL1PLDAWH"
        };
        foreach (var sku in required)
        {
            Assert.Contains(sku, all.Keys);
        }
    }

    [Fact]
    public void BomUsesCatalogNamesAndNumbers()
    {
        var catalog = new CatalogService();
        var input = new BuildingInput
        {
            Mode = "ROOM",
            Length = 10,
            Width = 10,
            Height = 12,
            PanelCoverageWidthFt = 1,
            Trims = new TrimSelections { JTrimEnabled = true }
        };
        var result = CalcService.CalcEstimate(input);
        var bom = BomService.Build(input, result, catalog, out var missing);
        Assert.False(missing);
        var j = Assert.Single(bom.Where(b => b.Category == "J"));
        Assert.Equal("GEL1TJCBBW", j.PartNumber);
        Assert.Contains("J Trim", j.Name);
    }
}
