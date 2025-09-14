namespace Nuform.Core.Domain;

// Patched version of BomLineItem with overage support.  The Overage field
// allows downstream consumers (e.g. the UI) to display and adjust the
// difference between the total provided linear footage (or quantity) and the
// required amount.  If no overage is computed, this value defaults to 0.
public class BomLineItem
{
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Overage { get; set; } = 0m;
}