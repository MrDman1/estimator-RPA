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
        /// Widthwise orientation: panels run across the width; panels/row is driven by building LENGTH.
        /// H-Trim runs along building length between rows.
        /// </summary>
        public static CeilingLayout ComputeWidthwise(double lengthFt, double widthFt, int panelWidthInches)
        {
            var panelsPerRow = CeilPanelsPerRow(lengthFt, panelWidthInches);
            var rows = (int)Math.Ceiling(widthFt / 20.0);
            if (rows < 1) rows = 1;
            var shipLen = RoundUpToStandard(widthFt / rows);
            var totalPanels = panelsPerRow * rows;
            var hTrimLF = Math.Max(0, rows - 1) * lengthFt;
            return new("Widthwise", rows, shipLen, panelsPerRow, totalPanels, hTrimLF);
        }

        /// <summary>
        /// Lengthwise orientation: panels run along the building length; panels/row is driven by building WIDTH.
        /// Choose rows/ship length to minimize cut waste.
        /// H-Trim runs along building width between rows.
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