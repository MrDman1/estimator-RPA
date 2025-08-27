using System;
using System.Collections.Generic;

namespace Nuform.Core.Domain;

public enum TrimKind
{
    J, H, F, InsideCorner, OutsideCorner, Transition, DripEdge, Cove, CrownBaseBase, CrownBaseCap
}

public static class TrimPolicy
{
    // pieces per package per trim kind (adjust if your catalog differs)
    public static readonly Dictionary<TrimKind, int> PiecesPerPackage = new()
    {
        [TrimKind.J] = 10,
        [TrimKind.F] = 10,
        [TrimKind.H] = 5,
        [TrimKind.InsideCorner] = 5,
        [TrimKind.OutsideCorner] = 5,
        [TrimKind.DripEdge] = 10,
        [TrimKind.Cove] = 5,
        [TrimKind.CrownBaseBase] = 5,
        [TrimKind.CrownBaseCap] = 5,
        [TrimKind.Transition] = 10
    };

    // Which color an LF contribution belongs to, given a wall & ceiling color.
    public static NuformColor ColorFor(TrimKind kind, NuformColor wallColor, NuformColor ceilingColor)
        => kind switch
        {
            TrimKind.Transition or TrimKind.DripEdge or TrimKind.Cove => ceilingColor,
            _ => wallColor
        };

    public static int DecideTrimLengthFeet(
        TrimKind kind,
        bool anyPanelOver12ft,     // true if any panel length > 12 for that area
        double requiredLF,         // LF required for this trim kind & color
        Func<int,int> piecesPerPkg // returns pieces per package for the given trim length (12 or 16)
    )
    {
        // Corners-first rule: if any panel > 12′, corners MUST be 16′. No waste override for corners.
        if ((kind == TrimKind.InsideCorner || kind == TrimKind.OutsideCorner) && anyPanelOver12ft)
            return 16;

        // If any panel > 12′, the "first choice" is 16′ for all trims.
        var initial = anyPanelOver12ft ? 16 : 12;
        if (initial == 12) return 12; // Panels ≤12′ → default 12′ for all trims.

        // Waste override: Only consider downgrading 16′ → 12′ (never the reverse).
        var pcs16 = piecesPerPkg(16);
        var pcs12 = piecesPerPkg(12);
        var packs16 = (int)Math.Ceiling(requiredLF / (pcs16 * 16.0));
        var packs12 = (int)Math.Ceiling(requiredLF / (pcs12 * 12.0));

        var waste16 = packs16 == 0 ? 0.0 : (packs16 * pcs16 * 16.0 - requiredLF) / (packs16 * pcs16 * 16.0);
        var waste12 = packs12 == 0 ? 0.0 : (packs12 * pcs12 * 12.0 - requiredLF) / (packs12 * pcs12 * 12.0);

        // Rule: if 16′ waste > 55% AND 12′ waste ≤ 40%, use 12′ (except corners – already handled).
        if (waste16 > 0.55 && waste12 <= 0.40 && kind is not TrimKind.InsideCorner and not TrimKind.OutsideCorner)
            return 12;

        return 16;
    }
}

