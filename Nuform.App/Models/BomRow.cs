using System;
using System.ComponentModel;

namespace Nuform.App.Models
{
    /// <summary>
    /// Canonical BOM row model that keeps base vs. suggested and exposes
    /// a unified overage in both units and percent. "Change" is always in
    /// the SAME UNITS as SuggestedQty (packs/boxes/panels).
    /// </summary>
    public sealed class BomRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged(string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Display / identity
        public string PartNumber { get; init; } = string.Empty;
        public string Name       { get; init; } = string.Empty;
        public string Unit       { get; init; } = string.Empty; // e.g., PCS, pkg, pcs, LF (display only)
        public string Category   { get; init; } = string.Empty;

        /// <summary>
        /// Quantity the grid shows (already includes contingency/rounding for panels,
        /// and is in PACKS/BOXES for trim/screws/accessories).
        /// </summary>
        public decimal SuggestedQty { get; init; }

        /// <summary>
        /// Base quantity BEFORE contingency/waste/rounding, expressed in the
        /// same units as SuggestedQty (packs/boxes/panels). Used as denominator
        /// for percentage overage.
        /// </summary>
        public decimal BaseQtyUnits { get; init; }

        /// <summary>
        /// Initial overage (Suggested - Base) in SAME UNITS as SuggestedQty.
        /// This makes the "hidden" contingency/rounding visible.
        /// </summary>
        public decimal InitialOverageUnits { get; init; }

        /// <summary>
        /// Free-form metadata so you can display/inspect how packs are derived (optional).
        /// Examples: "LF", "pcs", "".
        /// </summary>
        public string PackBasis { get; init; } = string.Empty;

        /// <summary>
        /// Pack/box size if applicable (e.g., 160 LF/pack, 500 pcs/box). Optional.
        /// </summary>
        public decimal? PackSize { get; init; }

        // === Editable delta (kept in SAME UNITS as SuggestedQty) ===
        private string _change = "0";
        public string Change
        {
            get => _change;
            set
            {
                if (_change == value) return;
                _change = value;
                OnChanged(nameof(Change));
                OnChanged(nameof(FinalQty));
                OnChanged(nameof(OverageUnits));
                OnChanged(nameof(OveragePercent));
            }
        }

        /// <summary>
        /// Parses Change. Accepts "+3", "-1.5", "5%", "-12.5%".
        /// Percent is relative to SuggestedQty.
        /// </summary>
        private decimal ParseDeltaUnits()
        {
            if (string.IsNullOrWhiteSpace(_change)) return 0m;
            var s = _change.Trim();
            if (s.StartsWith("+")) s = s.Substring(1);

            var isPercent = s.EndsWith("%", StringComparison.Ordinal);
            if (isPercent) s = s[..^1].Trim();

            if (!decimal.TryParse(s, out var val)) return 0m;

            if (isPercent)
            {
                return SuggestedQty == 0m ? 0m : (val / 100m) * SuggestedQty;
            }

            // Numeric = same units as SuggestedQty (packs/boxes/panels)
            return val;
        }

        /// <summary>
        /// The final quantity after user change, in SAME UNITS as Suggested.
        /// </summary>
        public decimal FinalQty => SuggestedQty + ParseDeltaUnits();

        /// <summary>
        /// Total overage in units relative to Base (Initial + Change).
        /// </summary>
        public decimal OverageUnits => InitialOverageUnits + ParseDeltaUnits();

        /// <summary>
        /// Total overage in percent relative to Base. Shows the hidden
        /// contingency/rounding AND any user edits.
        /// </summary>
        public decimal OveragePercent =>
            BaseQtyUnits <= 0m ? 0m : (OverageUnits / BaseQtyUnits) * 100m;
    }
}
