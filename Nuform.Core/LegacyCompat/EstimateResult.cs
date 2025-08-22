using System.Collections.Generic;

namespace Nuform.Core.LegacyCompat;

/// <summary>
/// Compatibility estimate output used by legacy utilities.
/// Internal note of referenced members:
/// - <see cref="WallPanels"/> : Dictionary&lt;double,int&gt;
/// - <see cref="CeilingPanels"/> : Dictionary&lt;double,int&gt;
/// - <see cref="Trims"/> : <see cref="TrimResult"/>
/// - <see cref="Hardware"/> : <see cref="HardwareResult"/>
/// - <see cref="Parts"/> : List&lt;<see cref="PartRequirement"/>&gt;
/// - <see cref="Rooms"/> : List&lt;<see cref="Room"/>&gt;
/// </summary>
public sealed class EstimateResult
{
    public Dictionary<double, int> WallPanels { get; set; } = new();
    public Dictionary<double, int> CeilingPanels { get; set; } = new();
    public TrimResult Trims { get; set; } = new();
    public HardwareResult Hardware { get; set; } = new();
    public List<PartRequirement> Parts { get; set; } = new();
    public List<Room> Rooms { get; set; } = new();
}

public sealed class TrimResult
{
    public int JTrimPacks { get; set; }
    public int JTrimPackLenFt { get; set; }
    public int CornerPacks { get; set; }
    public int CornerPackLenFt { get; set; }
    public int CrownBasePairs { get; set; }
    public int TopTrackPackLenFt { get; set; }
}

public sealed class HardwareResult
{
    public int PlugSpacerPacks { get; set; }
    public int ExpansionTools { get; set; }
    public int ScrewBoxes { get; set; }
    public int WallScrewBoxes { get; set; }
    public int CeilingScrewBoxes { get; set; }
}

public sealed class PartRequirement
{
    public string PartCode { get; set; } = string.Empty;
    public int QtyPacks { get; set; }
    public double LFNeeded { get; set; }
}

