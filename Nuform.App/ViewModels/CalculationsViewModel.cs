using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Nuform.Core.Domain;
using VmEstimateState = Nuform.App.ViewModels.EstimateState;
using DomainOpeningTreatment = Nuform.Core.Domain.OpeningTreatment;
using DomainCeilingOrientation = Nuform.Core.Domain.CeilingOrientation;
using DomainNuformColor = Nuform.Core.Domain.NuformColor;

namespace Nuform.App.ViewModels;

public sealed class CalculationsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string? n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    private readonly VmEstimateState _state;
    private CalcEstimateResult _last;

    public string CalculationsText { get; private set; } = string.Empty;

    public CalculationsViewModel(VmEstimateState state)
    {
        _state = state;
        _last = CalcService.CalcEstimate(_state.Input);
        _state.Result = _last;
        BuildText();
    }

    private void BuildText()
    {
        var input = _state.Input;
        var sb = new StringBuilder();

        var L = (decimal)input.Length;
        var W = (decimal)input.Width;
        var perim = input.Mode == "ROOM" ? 2m * (L + W) : L;

        double openingsWidthLF = input.Openings.Where(o => o.Treatment == DomainOpeningTreatment.BUTT)
            .Sum(o => o.Width * o.Count);
        double panelWidthFt = input.WallPanelWidthInches == 18 ? 1.5 : 1.0;
        double headerLFAdd = input.Openings.Where(o => o.Treatment == DomainOpeningTreatment.BUTT)
            .Sum(o =>
            {
                double headerAndSill = Math.Max(0, o.HeaderHeightFt) + Math.Max(0, o.SillHeightFt);
                if (headerAndSill <= 0) return 0.0;
                double piecesPerFull = (double)input.WallPanelLengthFt / headerAndSill;
                if (piecesPerFull <= 0) return 0.0;
                double headerPanelsAdded = (o.Width * o.Count) / piecesPerFull;
                return headerPanelsAdded * panelWidthFt;
            });
        double netLF = (double)perim - openingsWidthLF + headerLFAdd;

        double extrasPct = input.ExtraPercent ?? CalcSettings.DefaultExtraPercent;
        double withExtras = netLF * (1 + extrasPct / 100.0);
        int basePanels = (int)Math.Ceiling(withExtras / panelWidthFt);
        int roundedPanels = CalcService.RoundPanels(basePanels);

        sb.AppendLine("WALL PANELS");
        sb.AppendLine($"Perimeter P = Σ2×(L+W) = {perim} ft");
        sb.AppendLine($"Minus opening widths W_open = Σ(width×count) = {openingsWidthLF} ft");
        sb.AppendLine($"Header add-back H_add: piecesPerFull = panelLen/(header+sill); headerPanelsAdded = width/piecesPerFull; H_add = headerPanelsAdded×{panelWidthFt:F1} = {headerLFAdd:F1} ft");
        sb.AppendLine($"Net LF = P − W_open + H_add = {netLF:F1} ft");
        sb.AppendLine($"Base panels = ceil(Net/{panelWidthFt:F1}) × (1+{extrasPct}%) = {basePanels}");
        sb.AppendLine($"Rounded = {roundedPanels}  (even≤150, 5s>150)");
        sb.AppendLine();

        sb.AppendLine("Panels (by color):");
        var wallPanelBreakdown = new List<(string color, decimal lenFt, int cnt)>
        {
            (input.WallPanelColor, input.WallPanelLengthFt, roundedPanels)
        };
        foreach (var (color, lenFt, cnt) in wallPanelBreakdown)
            sb.AppendLine($"  {color}: length {lenFt}′ → {cnt} panels");
        sb.AppendLine();

        double openingsButtPerimeter = 0;
        double openingsWrappedPerimeter = 0;
        foreach (var op in input.Openings)
        {
            var per = 2 * (op.Width + op.Height) * op.Count;
            if (op.Treatment == DomainOpeningTreatment.WRAPPED) openingsWrappedPerimeter += per;
            else openingsButtPerimeter += per;
        }

        var wallColor = PanelCodeResolver.ParseColor(input.WallPanelColor);
        var ceilingColor = PanelCodeResolver.ParseColor(input.CeilingPanelColor);
        var wallAnyPanelOver12 = (double)input.WallPanelLengthFt > 12;
        var ceilingAnyPanelOver12 = (double)input.CeilingPanelLengthFt > 12;

        var wallTrimLF = new Dictionary<(TrimKind, DomainNuformColor), double>();
        var ceilingTrimLF = new Dictionary<(TrimKind, DomainNuformColor), double>();

        // Wrapped openings: J + Outside Corner
        AddLF(wallTrimLF, (TrimKind.J, wallColor), openingsWrappedPerimeter);
        AddLF(wallTrimLF, (TrimKind.OutsideCorner, wallColor), openingsWrappedPerimeter);

        // Butt openings
        AddLF(wallTrimLF, (TrimKind.J, wallColor), openingsButtPerimeter);

        // Base J for walls
        AddLF(wallTrimLF, (TrimKind.J, wallColor), (double)perim);

        // Inside corners
        var insideCorners = CalcService.ComputeInsideCorners(input);
        AddLF(wallTrimLF, (TrimKind.InsideCorner, wallColor), insideCorners * input.Height);

        // Ceiling transition trims
        if (input.Trims.CeilingTransition != null)
        {
            var lf = (double)perim;
            switch (input.Trims.CeilingTransition)
            {
                case "cove":
                    AddLF(ceilingTrimLF, (TrimKind.Cove, ceilingColor), lf);
                    break;
                case "crown-base":
                    AddLF(ceilingTrimLF, (TrimKind.CrownBaseBase, ceilingColor), lf);
                    AddLF(ceilingTrimLF, (TrimKind.CrownBaseCap, ceilingColor), lf);
                    break;
                case "f-trim":
                    AddLF(ceilingTrimLF, (TrimKind.Transition, ceilingColor), lf);
                    break;
            }
        }

        sb.AppendLine("Trims (LF → packages by color):");
        foreach (var kv in wallTrimLF)
        {
            var kind = kv.Key.Item1; var color = kv.Key.Item2; var lf = kv.Value;
            var anyOver12 = wallAnyPanelOver12;
            var decidedLen = TrimPolicy.DecideTrimLengthFeet(kind, anyOver12, lf, _ => TrimPolicy.PiecesPerPackage[kind]);
            var pcs = TrimPolicy.PiecesPerPackage[kind];
            var packs16 = (int)Math.Ceiling(lf / (pcs * 16.0));
            var waste16 = packs16 == 0 ? 0.0 : (packs16 * pcs * 16.0 - lf) / (packs16 * pcs * 16.0);
            var packs12 = (int)Math.Ceiling(lf / (pcs * 12.0));
            var waste12 = packs12 == 0 ? 0.0 : (packs12 * pcs * 12.0 - lf) / (packs12 * pcs * 12.0);
            sb.AppendLine($"  {PanelCodeResolver.ColorName(color)} {kind}: LF={lf:F1} → try 16′: packs={packs16}, waste={waste16:P0}; try 12′: packs={packs12}, waste={waste12:P0} → chosen {decidedLen}′");
        }

        sb.AppendLine();
        sb.AppendLine("CEILING PANELS");
        if (input.IncludeCeilingPanels)
        {
            double cPanelWidthFt = input.CeilingPanelWidthInches == 18 ? 1.5 : 1.0;
            int ceilingLenFt = (int)input.CeilingPanelLengthFt;
            if (input.CeilingOrientation == DomainCeilingOrientation.Widthwise)
            {
                int rows = (int)Math.Ceiling(input.Length / cPanelWidthFt);
                int panelsWidthwise = rows;
                sb.AppendLine($"Widthwise: rows = ceil(L/{cPanelWidthFt:F1}) = {rows}; panels = rows = {panelsWidthwise}");
            }
            else
            {
                int rows = (int)Math.Ceiling(input.Width / cPanelWidthFt);
                int panelsPerRow = (int)Math.Ceiling(input.Length / (double)ceilingLenFt);
                int panelsLengthwise = rows * panelsPerRow;
                sb.AppendLine($"Lengthwise: rows = ceil(W/{cPanelWidthFt:F1}) = {rows}; panels/row = ceil(L/{ceilingLenFt}) = {panelsPerRow}; total = rows×panels/row = {panelsLengthwise}");
            }
        }

        foreach (var kv in ceilingTrimLF)
        {
            var kind = kv.Key.Item1; var color = kv.Key.Item2; var lf = kv.Value;
            var anyOver12 = ceilingAnyPanelOver12;
            var decidedLen = TrimPolicy.DecideTrimLengthFeet(kind, anyOver12, lf, _ => TrimPolicy.PiecesPerPackage[kind]);
            var pcs = TrimPolicy.PiecesPerPackage[kind];
            var packs16 = (int)Math.Ceiling(lf / (pcs * 16.0));
            var waste16 = packs16 == 0 ? 0.0 : (packs16 * pcs * 16.0 - lf) / (packs16 * pcs * 16.0);
            var packs12 = (int)Math.Ceiling(lf / (pcs * 12.0));
            var waste12 = packs12 == 0 ? 0.0 : (packs12 * pcs * 12.0 - lf) / (packs12 * pcs * 12.0);
            sb.AppendLine($"  {PanelCodeResolver.ColorName(color)} {kind}: LF={lf:F1} → try 16′: packs={packs16}, waste={waste16:P0}; try 12′: packs={packs12}, waste={waste12:P0} → chosen {decidedLen}′");
        }

        CalculationsText = sb.ToString();
        OnPropertyChanged(nameof(CalculationsText));
    }

    private static void AddLF(Dictionary<(TrimKind, DomainNuformColor), double> map, (TrimKind, DomainNuformColor) key, double lf)
    {
        if (lf <= 0) return;
        map.TryGetValue(key, out var cur);
        map[key] = cur + lf;
    }
}

