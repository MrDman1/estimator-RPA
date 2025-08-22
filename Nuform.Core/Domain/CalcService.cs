using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Nuform.Core.Domain;

public enum OpeningTreatment { WRAPPED, BUTT }

public class OpeningInput
{
    public string Type { get; set; } = string.Empty; // garage, man, window, custom
    public double Width { get; set; }
    public double Height { get; set; }
    public int Count { get; set; }
    public OpeningTreatment Treatment { get; set; } = OpeningTreatment.BUTT;
}

public class TrimSelections
{
    public bool JTrimEnabled { get; set; } = true;
    // values: "crown-base", "cove", "f-trim" or null
    public string? CeilingTransition { get; set; }
}

    public class BuildingInput
    {
        // "ROOM" or "WALL"
        public string Mode { get; set; } = "ROOM";
        public double Length { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public List<OpeningInput> Openings { get; set; } = new();
        // effective panel coverage width in ft
        public double PanelCoverageWidthFt { get; set; } = 1.0;
        public double? ExtraPercent { get; set; }
        public TrimSelections Trims { get; set; } = new();

        // New panel selection fields
        public bool IncludeCeilingPanels { get; set; } = false;

        // Selected wall panel spec
        public string WallPanelSeries { get; set; } = "R3";       // "R3" | "Mono" | "Pro18"
        public int WallPanelWidthInches { get; set; } = 12;        // 12 or 18
        public decimal WallPanelLengthFt { get; set; } = 12m;      // 8.5, 10, 12, 14, 16, 18, 20
        public string WallPanelColor { get; set; } = "NUFORM WHITE";

        // Selected ceiling panel spec (can differ from wall)
        public string CeilingPanelSeries { get; set; } = "R3";
        public int CeilingPanelWidthInches { get; set; } = 12;     // 12 or 18
        public decimal CeilingPanelLengthFt { get; set; } = 12m;
        public string CeilingPanelColor { get; set; } = "NUFORM WHITE";
    }

public class PanelCalcResult
{
    public int BasePanels { get; set; }
    public double ExtraPercentApplied { get; set; }
    public int RoundedPanels { get; set; }
    public double OveragePercentRounded { get; set; }
    public bool WarnExceedsConfigured { get; set; }
    public bool ManualExtraOverride { get; set; }
}

public class TrimCalcResult
{
    public double JTrimLF { get; set; }
    public double CeilingTrimLF { get; set; }
    public string? CeilingTransition { get; set; }
}

public class CalcEstimateResult
{
    public PanelCalcResult Panels { get; set; } = new();
    public TrimCalcResult Trims { get; set; } = new();
    public int InsideCorners { get; set; }
}

public static class CalcSettings
{
    public const double DefaultExtraPercent = 5.0;
    public const double WarnWhenRoundedExceedsPercent = 7.5;
}

public static class CalcService
{
    public static int ComputeInsideCorners(BuildingInput input)
    {
        if (input.Mode == "ROOM")
        {
            if (input.Length > 1 && input.Width > 1) return 4;
            if (input.Width == 1) return 0; // single wall
        }
        return 0;
    }

    public static int RoundPanels(double qty)
        => qty <= 150 ? (int)Math.Ceiling(qty / 2.0) * 2 : (int)Math.Ceiling(qty / 5.0) * 5;

    public static CalcEstimateResult CalcEstimate(BuildingInput input)
    {
        double perimeter = input.Mode == "ROOM"
            ? 2 * (input.Length + input.Width)
            : input.Length;

        double openingsArea = 0;
        double openingsPerimeterLF = 0;
        foreach (var op in input.Openings)
        {
            double area = op.Width * op.Height * op.Count;
            if (op.Treatment == OpeningTreatment.BUTT)
            {
                openingsArea += area;
                openingsPerimeterLF += 2 * (op.Width + op.Height) * op.Count;
            }
        }

        double wallArea = perimeter * input.Height;
        double netWallArea = wallArea - openingsArea;
        int basePanels = (int)Math.Ceiling(netWallArea / (input.PanelCoverageWidthFt * input.Height));

        double extraPercent = input.ExtraPercent ?? CalcSettings.DefaultExtraPercent;
        double withExtra = basePanels * (1 + extraPercent / 100.0);
        int roundedPanels = RoundPanels(withExtra);
        double overagePercentRounded = ((roundedPanels - basePanels) / (double)basePanels) * 100.0;
        bool warnExceeds = overagePercentRounded > CalcSettings.WarnWhenRoundedExceedsPercent;
        bool manualOverride = extraPercent != CalcSettings.DefaultExtraPercent;

        double jTrimLF = 0;
        double ceilingTrimLF = 0;
        if (input.Trims.JTrimEnabled)
        {
            double multiplier = input.Trims.CeilingTransition != null ? 1 : 3;
            jTrimLF = multiplier * perimeter + openingsPerimeterLF;
        }
        if (input.Trims.CeilingTransition != null)
        {
            ceilingTrimLF = perimeter;
        }

        return new CalcEstimateResult
        {
            Panels = new PanelCalcResult
            {
                BasePanels = basePanels,
                ExtraPercentApplied = extraPercent,
                RoundedPanels = roundedPanels,
                OveragePercentRounded = overagePercentRounded,
                WarnExceedsConfigured = warnExceeds,
                ManualExtraOverride = manualOverride
            },
            Trims = new TrimCalcResult
            {
                JTrimLF = jTrimLF,
                CeilingTrimLF = ceilingTrimLF,
                CeilingTransition = input.Trims.CeilingTransition
            },
            InsideCorners = ComputeInsideCorners(input)
        };
    }
}
