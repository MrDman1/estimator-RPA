using System;
using System.ComponentModel;

namespace Nuform.App.Models;

// Patched version of BomRow.cs with Overage property.
public sealed class BomRow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    public string PartNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;

    public decimal SuggestedQty { get; init; }

    private string _change = "0"; // accepts +5, -2, 3
    public string Change
    {
        get => _change;
        set
        {
            if (_change == value) return;
            _change = value;
            OnChanged(nameof(Change));
            OnChanged(nameof(FinalQty));
        }
    }

    public decimal FinalQty
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_change)) return SuggestedQty;
            var s = _change.Trim();
            if (s.StartsWith("+")) s = s.Substring(1);
            if (!decimal.TryParse(s, out var delta))
                return SuggestedQty; // invalid text ignored
            if (_change.StartsWith("-")) delta = -Math.Abs(delta);
            return SuggestedQty + delta;
        }
    }

    /// <summary>
    /// Extra quantity suggested beyond the base requirement.  For panel lines this
    /// value represents the number of additional panels (rounded minus base).
    /// For trim and accessory lines it defaults to 0 because the underlying BOM
    /// does not expose raw linear-footage calculations.  Consumers may bind to
    /// this property to display overage per line item and allow manual
    /// adjustment similar to the Change column.
    /// </summary>
    public decimal Overage { get; init; }
}