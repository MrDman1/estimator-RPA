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
    /// For trim and accessory lines it reflects the excess linear footage or pieces
    /// provided by rounding up to full packages.  This property can be edited by
    /// the user to adjust overage per line item.  When updated, it raises
    /// PropertyChanged so the UI reflects the change immediately.  Note that
    /// changing overage does not automatically recompute SuggestedQty; it merely
    /// stores the user’s override for display.
    /// </summary>
    private decimal _overage;
    public decimal Overage
    {
        get => _overage;
        set
        {
            if (_overage == value) return;
            _overage = value;
            OnChanged(nameof(Overage));
            // Overages do not change FinalQty directly.  A UI could bind to this
            // property and implement further logic if needed.
        }
    }
}