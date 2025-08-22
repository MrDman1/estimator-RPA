using System.Collections.Generic;

namespace Nuform.Core.LegacyCompat;

public class EstimateResult
{
    public int BasePanels { get; set; }
    public int RoundedPanels { get; set; }
    public decimal OveragePercentRounded { get; set; }
    public bool WarnOverage { get; set; }
    public decimal ExtrasPercent { get; set; }
    public Dictionary<double, int> WallPanels { get; set; } = new();
    public Dictionary<double, int> CeilingPanels { get; set; } = new();
    public List<Room> Rooms { get; set; } = new();
    public HardwareResult Hardware { get; set; } = new();
    public List<BomLine> Parts { get; set; } = new();
}

public class BomLine
{
    public string PartCode { get; set; } = string.Empty;
    public int QtyPacks { get; set; }
    public double LFNeeded { get; set; }
}

public class HardwareResult
{
    public int PlugSpacerPacks { get; set; }
    public int ExpansionTools { get; set; }
    public int ScrewBoxes { get; set; }
    public int WallScrewBoxes { get; set; }
    public int CeilingScrewBoxes { get; set; }
}
