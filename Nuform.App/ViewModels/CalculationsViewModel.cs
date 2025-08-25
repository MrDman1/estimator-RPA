using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Nuform.Core.Domain;

namespace Nuform.App.ViewModels;

public sealed class CalculationsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string? n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    private readonly EstimateState _state;
    private CalcEstimateResult _last;

    public string CalculationsText { get; private set; } = string.Empty;

    public CalculationsViewModel(EstimateState state)
    {
        _state = state;
        _last = CalcService.CalcEstimate(_state.Input);
        _state.Result = _last;
        BuildText();
    }

    private void BuildText()
    {
        var input = _state.Input;
        var result = _last;
        var sb = new StringBuilder();

        var L = (decimal)input.Length;
        var W = (decimal)input.Width;
        var perim = input.Mode == "ROOM" ? 2m * (L + W) : L;

        double openingsButtPerimeter = 0;
        double openingsWrappedPerimeter = 0;
        foreach (var op in input.Openings)
        {
            var per = 2 * (op.Width + op.Height) * op.Count;
            if (op.Treatment == OpeningTreatment.WRAPPED) openingsWrappedPerimeter += per;
            else openingsButtPerimeter += per;
        }

        double headerLF = 0; // headers not modeled separately
        var netLF = (double)perim - openingsButtPerimeter + headerLF;

        sb.AppendLine("WALLS");
        sb.AppendLine($"Perimeter: P = 2×(L+W) = {L}+{W} → {perim} ft");
        sb.AppendLine($"Openings (BUTT): Op = Σ2×(w+h)×count = {openingsButtPerimeter} ft");
        sb.AppendLine($"Openings (WRAPPED): Wr = Σ2×(w+h)×count = {openingsWrappedPerimeter} ft");
        sb.AppendLine($"Headers: H = Σ(width×count) = {headerLF} ft");
        sb.AppendLine($"Net LF: Net = P − Op + H = {perim} − {openingsButtPerimeter} + {headerLF} = {netLF} ft");
        sb.AppendLine();

        sb.AppendLine("Panels (by color):");
        var wallPanelBreakdown = new List<(string color, decimal lenFt, int cnt)>
        {
            (input.WallPanelColor, input.WallPanelLengthFt, result.Panels.RoundedPanels)
        };
        foreach (var (color, lenFt, cnt) in wallPanelBreakdown)
            sb.AppendLine($"  {color}: length {lenFt}′ → {cnt} panels");
        sb.AppendLine();

        var wallColor = PanelCodeResolver.ParseColor(input.WallPanelColor);
        var ceilingColor = PanelCodeResolver.ParseColor(input.CeilingPanelColor);
        var wallAnyPanelOver12 = (double)input.WallPanelLengthFt > 12;
        var ceilingAnyPanelOver12 = (double)input.CeilingPanelLengthFt > 12;

        var wallTrimLF = new Dictionary<(TrimKind, NuformColor), double>();
        var ceilingTrimLF = new Dictionary<(TrimKind, NuformColor), double>();

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
        sb.AppendLine("CEILINGS");
        var panelWidthInches = input.CeilingPanelWidthInches;
        var widthDiv = panelWidthInches == 18 ? 1.5 : 1.0;
        var rows = input.IncludeCeilingPanels ? Math.Ceiling(input.Width / widthDiv) : 0;
        var panelsPerRow = input.IncludeCeilingPanels ? Math.Ceiling(input.Length / (double)input.CeilingPanelLengthFt) : 0;
        var totalCeilingPanels = rows * panelsPerRow;
        sb.AppendLine($"Width rows: rows = width / {(panelWidthInches == 18 ? "1.5" : "1.0")} = {rows}");
        sb.AppendLine($"Panels/row: len / panelLen = {input.Length} / {input.CeilingPanelLengthFt} = {panelsPerRow}");
        sb.AppendLine($"Total: rows×panels/row = {rows}×{panelsPerRow} = {totalCeilingPanels}");

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

        sb.AppendLine();
        sb.AppendLine("Panels");
        sb.AppendLine($"Extras % = {input.ExtraPercent ?? CalcSettings.DefaultExtraPercent}");
        sb.AppendLine($"Base = {result.Panels.BasePanels}");
        sb.AppendLine($"Rounded = {result.Panels.RoundedPanels} (Overage = {result.Panels.OveragePercentRounded:N1}%)");

        CalculationsText = sb.ToString();
        OnPropertyChanged(nameof(CalculationsText));
    }

    private static void AddLF(Dictionary<(TrimKind, NuformColor), double> map, (TrimKind, NuformColor) key, double lf)
    {
        if (lf <= 0) return;
        map.TryGetValue(key, out var cur);
        map[key] = cur + lf;
    }
}

