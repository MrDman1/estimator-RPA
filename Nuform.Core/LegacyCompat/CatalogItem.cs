namespace Nuform.Core.LegacyCompat;

public class CatalogItem
{
    public string PartCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int PackPieces { get; set; }
    public decimal LengthFt { get; set; }
    public string Color { get; set; } = string.Empty;
    public decimal PriceUSD { get; set; }
}
