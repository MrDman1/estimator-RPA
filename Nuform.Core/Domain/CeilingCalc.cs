using System;

namespace Nuform.Core.Domain
{
    public sealed record CeilingLayout(
        string Orientation,   // "Widthwise" | "Lengthwise"
        int Rows,
        int ShipLengthFt,     // length of each shipped panel
        int PanelsPerRow,
        int TotalPanels,
        double HTrimLF        // linear feet of H (before extras)
    );

    public static class CeilingCalc
    {
        static readonly int[] StdLens = new[] { 10, 12, 14, 16, 18, 20 };

        static int RoundUpToStandard(double ft)
        {
            foreach (var s in StdLens) if (ft <= s) return s;
            return 20;
        }

        static int CeilPanelsPerRow(double runFt, int panelWidthInches) // 12 or 18
        {
            var ftPerPanel = panelWidthInches == 18 ? 1.5 : 1.0;
            return (int)Math.Ceiling(runFt / ftPerPanel);
        }

        /// <summary>
        /// Widthwise orientation: panels run across the width; panels per row
        /// are driven by the building length.  This patched version follows
        /// the updated Nuform algorithm:
        ///  * If width > 25 ft, defer to the lengthwise calculation (multiple rows).
        ///  * If width > 20 ft and <= 25 ft, use a custom ship length equal to
        ///    the width (rounded up) and a single row.
        ///  * Otherwise, pick the nearest even standard length >= width and
        ///    use a single row.  H-trim is not required when there is only one row.
        /// </summary>
        public static CeilingLayout ComputeWidthwise(double lengthFt, double widthFt, int panelWidthInches)
        {
            // Defer to lengthwise when width exceeds 25 ft.
            if (widthFt > 25.0)
                return ComputeLengthwise(lengthFt, widthFt, panelWidthInches);

            // Compute the number of panels per row using the building length.
            var panelsPerRow = CeilPanelsPerRow(lengthFt, panelWidthInches);

            int rows = 1;
            int shipLen;

            if (widthFt > 20.0)
            {
                // For widths greater than 20 ft and up to 25 ft, propose a custom
                // shipping length equal to the width rounded up to the nearest foot.
                shipLen = (int)Math.Ceiling(widthFt);
            }
            else
            {
                // Otherwise, choose the next even standard length at least as long as the width.
                shipLen = RoundUpToStandard(widthFt);
            }

            // Total panels equals panels per row since rows=1; no H-trim needed.
            var totalPanels = panelsPerRow * rows;
            double hTrimLF = 0.0;
            return new("Widthwise", rows, shipLen, panelsPerRow, totalPanels, hTrimLF);
        }

        /// <summary>
        /// Lengthwise orientation: panels run along the building length; panels/row
        /// is driven by building width.  Choose rows/ship length to minimize waste.
        /// </summary>
        public static CeilingLayout ComputeLengthwise(double lengthFt, double widthFt, int panelWidthInches)
        {
            var panelsPerRow = CeilPanelsPerRow(widthFt, panelWidthInches);

            var minRows = (int)Math.Ceiling(lengthFt / 20.0);
            var maxRows = (int)Math.Ceiling(lengthFt / 10.0);
            if (minRows < 1) minRows = 1;
            if (maxRows < minRows) maxRows = minRows;

            int bestRows = minRows, bestShip = 20;
            double bestWaste = double.MaxValue;

            for (int r = minRows; r <= maxRows; r++)
            {
                var target = lengthFt / r;
                var shipLen = RoundUpToStandard(target);
                var waste = r * shipLen - lengthFt;
                var better = waste < bestWaste || (Math.Abs(waste - bestWaste) < 1e-9 && shipLen > bestShip);
                if (better) { bestWaste = waste; bestShip = shipLen; bestRows = r; }
            }

            var totalPanels = panelsPerRow * bestRows;
            var hTrimLF = Math.Max(0, bestRows - 1) * widthFt;
            return new("Lengthwise", bestRows, bestShip, panelsPerRow, totalPanels, hTrimLF);
        }
    }
}