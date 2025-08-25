namespace Nuform.Core.LegacyCompat;

public class Room
{
    public double LengthFt { get; set; }
    public double WidthFt { get; set; }
    public double HeightFt { get; set; }
    public double WallPanelLengthFt { get; set; }
    public double PanelWidthInches { get; set; } = 12;
    public bool HasCeiling { get; set; }
    public double CeilingPanelLengthFt { get; set; }
    public CeilingOrientation CeilingOrientation { get; set; } = CeilingOrientation.Lengthwise;

    // Per-room panel colors
    public Nuform.Core.Domain.NuformColor WallPanelColor { get; set; } = Nuform.Core.Domain.NuformColor.NuformWhite;
    public Nuform.Core.Domain.NuformColor CeilingPanelColor { get; set; } = Nuform.Core.Domain.NuformColor.NuformWhite;
}
