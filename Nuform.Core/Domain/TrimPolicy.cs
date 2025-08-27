using System;
using System.Collections.Generic;

namespace Nuform.Core.Domain
{
    public enum TrimKind
    {
        J, H, F, InsideCorner, OutsideCorner, Transition, DripEdge, Cove, CrownBaseBase, CrownBaseCap
    }

    /// <summary>
    /// Central place for trim packaging/waste rules.
    /// </summary>
    public static class TrimPolicy
    {
        /// <summary>Pieces per package for each trim kind.</summary>
        public static readonly Dictionary<TrimKind, int> PiecesPerPackage = new()
        {
            [TrimKind.J] = 10,
            [TrimKind.H] = 5,
            [TrimKind.F] = 10,
            [TrimKind.InsideCorner] = 5,
            [TrimKind.OutsideCorner] = 5,
            [TrimKind.Transition] = 10,
            [TrimKind.DripEdge] = 10,
            [TrimKind.Cove] = 10,
            [TrimKind.CrownBaseBase] = 10,
            [TrimKind.CrownBaseCap] = 10
        };

        /// <summary>
        /// Choose a 12′ or 16′ pack for trims. If 16′ waste would exceed 60% and 12′ is not worse, choose 12′.
        /// Corners are excluded (their length rules are usually fixed elsewhere).
        /// </summary>
        public static int ChooseTrimPackLength(TrimKind kind, double requiredLF)
        {
            int pcs = PiecesPerPackage[kind];

            (int packs, double wastePct) Calc(int lenFt)
            {
                var cap = pcs * lenFt;
                var packs = (int)Math.Ceiling(requiredLF / cap);
                var waste = packs <= 0 ? 0.0 : (packs * cap - requiredLF) / (packs * cap);
                return (packs, waste);
            }

            var (p16, w16) = Calc(16);
            var (p12, w12) = Calc(12);

            if (kind != TrimKind.InsideCorner && kind != TrimKind.OutsideCorner)
            {
                if (w16 > 0.60 && w12 <= w16) return 12;
            }
            return 16;
        }

        /// <summary>
        /// Decide whether to ship 12′ or 16′ trim for a given required LF.
        /// Signature matches existing callsites in BomService.
        /// </summary>
        public static int DecideTrimLengthFeet(TrimKind kind, bool anyPanelOver12, double requiredLF, Func<int,int> piecesPerPackProvider)
        {
            // Allow provider to vary by length if needed
            int pcs12 = piecesPerPackProvider != null ? piecesPerPackProvider(12) : PiecesPerPackage[kind];
            int pcs16 = piecesPerPackProvider != null ? piecesPerPackProvider(16) : PiecesPerPackage[kind];

            double cap12 = pcs12 * 12.0;
            double cap16 = pcs16 * 16.0;

            int packs12 = (int)Math.Ceiling(requiredLF / cap12);
            int packs16 = (int)Math.Ceiling(requiredLF / cap16);

            double waste12 = packs12 <= 0 ? 0.0 : (packs12 * cap12 - requiredLF) / (packs12 * cap12);
            double waste16 = packs16 <= 0 ? 0.0 : (packs16 * cap16 - requiredLF) / (packs16 * cap16);

            // Rule: if 16′ waste > 60% and 12′ is not worse, choose 12′.
            if (waste16 > 0.60 && waste12 <= waste16) return 12;

            // Otherwise choose the option with lower waste. Tie → prefer 16′ (fewer seams).
            if (Math.Abs(waste12 - waste16) < 1e-9) return 16;
            return waste12 < waste16 ? 12 : 16;
        }

}

}
