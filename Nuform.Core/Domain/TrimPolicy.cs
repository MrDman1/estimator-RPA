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
    }
}