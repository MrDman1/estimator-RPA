using System;
using System.ComponentModel;

namespace Nuform.App.Models;

public sealed class BomRow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string? n=null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    public string PartNumber { get; init; } = "";
    public string Name { get; init; } = "";
    public string Unit { get; init; } = "";
    public string Category { get; init; } = "";

    public decimal SuggestedQty { get; init; }

    private string _change = "0"; // accepts +5, -2, 3
    public string Change
    {
        get => _change;
        set { if (_change==value) return; _change = value; OnChanged(nameof(Change)); OnChanged(nameof(FinalQty)); }
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
}
