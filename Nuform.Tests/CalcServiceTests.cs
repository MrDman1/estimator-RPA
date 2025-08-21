using System.Collections.Generic;
using Nuform.Core.Domain;
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
}
