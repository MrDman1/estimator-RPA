using System.Collections.Generic;
using System.Linq;
using Nuform.Core.Domain;
using Nuform.Core.Services;
using Xunit;

namespace Nuform.Tests;

public class CalcServiceTests
{
    BuildingInput BaseInput() => new()
    {
        Mode = "WALL",
        Length = 10,
        Width = 1,
        Height = 10,
        PanelCoverageWidthFt = 1,
        Trims = new TrimSelections { JTrimEnabled = true }
    };

    [Fact]
    public void OverageThresholdLogic()
    {
        var result = CalcService.CalcEstimate(BaseInput());
        Assert.True(result.Panels.WarnExceedsConfigured);
    }

    [Fact]
    public void JTrimReductionWithCeilingTransition()
    {
        var inputBase = new BuildingInput
        {
            Mode = "ROOM",
            Length = 10,
            Width = 10,
            Height = 10,
            PanelCoverageWidthFt = 1,
            Trims = new TrimSelections { JTrimEnabled = true }
        };
        var baseRes = CalcService.CalcEstimate(inputBase);
        var withCeiling = new BuildingInput
        {
            Mode = "ROOM",
            Length = 10,
            Width = 10,
            Height = 10,
            PanelCoverageWidthFt = 1,
            Trims = new TrimSelections { JTrimEnabled = true, CeilingTransition = "cove" }
        };
        var ceilingRes = CalcService.CalcEstimate(withCeiling);
        Assert.Equal(120, baseRes.Trims.JTrimLF);
        Assert.Equal(40, ceilingRes.Trims.JTrimLF);
        Assert.Equal(40, ceilingRes.Trims.CeilingTrimLF);
    }

    [Fact]
    public void InsideCornerAutoRules()
    {
        var room = new BuildingInput
        {
            Mode = "ROOM",
            Length = 10,
            Width = 10,
            Height = 10,
            PanelCoverageWidthFt = 1,
            Trims = new TrimSelections { JTrimEnabled = true }
        };
        Assert.Equal(4, CalcService.ComputeInsideCorners(room));
        var singleWall = new BuildingInput
        {
            Mode = "ROOM",
            Length = 10,
            Width = 1,
            Height = 10,
            PanelCoverageWidthFt = 1,
            Trims = new TrimSelections { JTrimEnabled = true }
        };
        Assert.Equal(0, CalcService.ComputeInsideCorners(singleWall));
        var wallMode = new BuildingInput
        {
            Mode = "WALL",
            Length = 10,
            Width = 1,
            Height = 10,
            PanelCoverageWidthFt = 1,
            Trims = new TrimSelections { JTrimEnabled = true }
        };
        Assert.Equal(0, CalcService.ComputeInsideCorners(wallMode));
    }

    [Fact]
    public void WrappedVsButtOpenings()
    {
        var butt = new BuildingInput
        {
            Mode = "WALL",
            Length = 20,
            Width = 1,
            Height = 10,
            PanelCoverageWidthFt = 1,
            Openings = new List<OpeningInput>
            {
                new() { Type = "custom", Width = 5, Height = 10, Count = 1, Treatment = OpeningTreatment.BUTT }
            },
            Trims = new TrimSelections { JTrimEnabled = true }
        };
        var wrap = new BuildingInput
        {
            Mode = "WALL",
            Length = 20,
            Width = 1,
            Height = 10,
            PanelCoverageWidthFt = 1,
            Openings = new List<OpeningInput>
            {
                new() { Type = "custom", Width = 5, Height = 10, Count = 1, Treatment = OpeningTreatment.WRAPPED }
            },
            Trims = new TrimSelections { JTrimEnabled = true }
        };
        var buttRes = CalcService.CalcEstimate(butt);
        var wrapRes = CalcService.CalcEstimate(wrap);
        Assert.True(wrapRes.Panels.BasePanels > buttRes.Panels.BasePanels);
        Assert.Equal(90, buttRes.Trims.JTrimLF);
        Assert.Equal(60, wrapRes.Trims.JTrimLF);
    }

    [Fact]
    public void WallPanelsSubtractWidthAndAddHeaderBack()
    {
        var withoutHeader = new BuildingInput
        {
            Mode = "ROOM",
            Length = 10,
            Width = 10,
            Height = 10,
            PanelCoverageWidthFt = 1,
            WallPanelLengthFt = 12m,
            Openings = new List<OpeningInput>
            {
                new() { Type = "custom", Width = 2, Height = 3, Count = 1, Treatment = OpeningTreatment.BUTT }
            },
            Trims = new TrimSelections { JTrimEnabled = true }
        };
        var withHeader = new BuildingInput
        {
            Mode = "ROOM",
            Length = 10,
            Width = 10,
            Height = 10,
            PanelCoverageWidthFt = 1,
            WallPanelLengthFt = 12m,
            Openings = new List<OpeningInput>
            {
                new() { Type = "custom", Width = 2, Height = 3, Count = 1, Treatment = OpeningTreatment.BUTT, HeaderHeightFt = 1, SillHeightFt = 1 }
            },
            Trims = new TrimSelections { JTrimEnabled = true }
        };
        var resNoHeader = CalcService.CalcEstimate(withoutHeader);
        var resWithHeader = CalcService.CalcEstimate(withHeader);

        Assert.Equal(40, resNoHeader.Panels.BasePanels);
        Assert.Equal(41, resWithHeader.Panels.BasePanels);
        Assert.Equal(42, resWithHeader.Panels.RoundedPanels);
        Assert.True(resWithHeader.Panels.RoundedPanels > resNoHeader.Panels.RoundedPanels);
    }

    [Fact]
    public void CeilingOrientationFormulas()
    {
        var catalog = new CatalogService();

        var widthwise = new BuildingInput
        {
            Mode = "ROOM",
            Length = 17,
            Width = 14,
            Height = 10,
            PanelCoverageWidthFt = 1,
            WallPanelLengthFt = 16m,
            IncludeCeilingPanels = true,
            CeilingPanelWidthInches = 12,
            CeilingPanelLengthFt = 12m,
            CeilingOrientation = CeilingOrientation.Widthwise,
            Trims = new TrimSelections { JTrimEnabled = false }
        };
        var resW = CalcService.CalcEstimate(widthwise);
        var bomW = BomService.Build(widthwise, resW, catalog, out var missingW);
        var itemW = Assert.Single(bomW.Where(b => b.PartNumber.Contains("GEL2PLDA")));
        Assert.Equal(18m, itemW.Quantity);

        var lengthwise = new BuildingInput
        {
            Mode = "ROOM",
            Length = 17,
            Width = 14,
            Height = 10,
            PanelCoverageWidthFt = 1,
            WallPanelLengthFt = 16m,
            IncludeCeilingPanels = true,
            CeilingPanelWidthInches = 12,
            CeilingPanelLengthFt = 12m,
            CeilingOrientation = CeilingOrientation.Lengthwise,
            Trims = new TrimSelections { JTrimEnabled = false }
        };
        var resL = CalcService.CalcEstimate(lengthwise);
        var bomL = BomService.Build(lengthwise, resL, catalog, out var missingL);
        var itemL = Assert.Single(bomL.Where(b => b.PartNumber.Contains("GEL2PLCA")));
        Assert.Equal(30m, itemL.Quantity);
    }
}
