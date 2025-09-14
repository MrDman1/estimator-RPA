using System;
using System.ComponentModel;

namespace Nuform.App.Models;

// Patched version of BomRow.cs with Overage property.
public sealed class BomRow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string? n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    public string PartNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;

    public decimal SuggestedQty { get; init; }

    // The user-editable field. Accepts: "3", "+3", "-2", "5%", "-12.5%"
    private string _change = "0";
    public string Change
    {
        get => _change;
        set
        {
            if (_change == value) return;
            _change = value;
            OnChanged(nameof(Change));
            // change affects everything derived:
            OnChanged(nameof(FinalQty));
            OnChanged(nameof(OverageUnits));
            OnChanged(nameof(OveragePercent));     // for the percent column
            OnChanged(nameof(OveragePercentText)); // if you expose it in the grid
        }
    }

    // Parse 'Change' into a delta in UNITS.
    private decimal ParseDeltaUnits()
    {
        if (string.IsNullOrWhiteSpace(_change)) return 0m;
        var s = _change.Trim();

        // keep a leading '+' optional
        if (s.StartsWith("+")) s = s.Substring(1);

        var isPercent = s.EndsWith("%");
        if (isPercent) s = s.Substring(0, s.Length - 1).Trim();

        if (!decimal.TryParse(s, out var val)) return 0m;

        if (isPercent)
        {
            // percent of SuggestedQty
            return SuggestedQty == 0m ? 0m : (val / 100m) * SuggestedQty;
        }

        // units
        return val;
    }

    public decimal FinalQty => SuggestedQty + ParseDeltaUnits();

    // Unified overage in units, mirrors the same delta shown by Change:
    public decimal OverageUnits => ParseDeltaUnits();

    // Unified overage in percent, always derived from the same delta:
    public decimal OveragePercent =>
        SuggestedQty == 0m ? 0m : (OverageUnits / SuggestedQty) * 100m;

    // If you want to show a percent string in the grid:
    public string OveragePercentText
    {
        get => $"{OveragePercent:N1}%";
        set
        {
            // Allow editing the percent column to drive the same canonical delta.
            // Accepts "5", "5%", "-12.5", "-12.5%"
            if (string.IsNullOrWhiteSpace(value)) return;
            var s = value.Trim();
            if (s.StartsWith("+")) s = s.Substring(1);
            var endsWithPct = s.EndsWith("%");
            if (endsWithPct) s = s.Substring(0, s.Length - 1).Trim();

            if (!decimal.TryParse(s, out var pct)) return;

            // Convert to a Change that represents the same delta.
            // We’ll store 'Change' in percent form for clarity:
            var normalized = pct.ToString("0.####") + "%";
            if (_change != normalized)
            {
                _change = normalized;
                OnChanged(nameof(Change));
                OnChanged(nameof(FinalQty));
                OnChanged(nameof(OverageUnits));
                OnChanged(nameof(OveragePercent));
                OnChanged(nameof(OveragePercentText));
            }
        }
    }
}
