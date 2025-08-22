namespace Nuform.Core.Domain;

public class EstimateState
{
    public bool IncludeCeilingPanels { get; set; } = false;

    // Selected wall panel spec
    public string WallPanelSeries { get; set; } = "R3";       // "R3" | "Mono" | "Pro18"
    public int WallPanelWidthInches { get; set; } = 12;        // 12 or 18
    public decimal WallPanelLengthFt { get; set; } = 12m;      // 8.5, 10, 12, 14, 16, 18, 20
    public string WallPanelColor { get; set; } = "NUFORM WHITE";

    // Selected ceiling panel spec (can differ from wall)
    public string CeilingPanelSeries { get; set; } = "R3";    // may be different from wall
    public int CeilingPanelWidthInches { get; set; } = 12;     // 12 or 18
    public decimal CeilingPanelLengthFt { get; set; } = 12m;
    public string CeilingPanelColor { get; set; } = "NUFORM WHITE";
}
