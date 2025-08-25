using System;
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

        var P = input.Mode == "ROOM"
            ? 2m * ((decimal)input.Length + (decimal)input.Width)
            : (decimal)input.Length;
        sb.AppendLine("Perimeter");
        sb.AppendLine($"P = 2×(L+W) = {input.Length}+{input.Width} … = {P} ft");
        sb.AppendLine();

        decimal opPerim = 0m;
        foreach (var op in input.Openings)
            if (op.Treatment == OpeningTreatment.BUTT)
                opPerim += 2m * ((decimal)op.Width + (decimal)op.Height) * op.Count;
        sb.AppendLine("Openings Perimeter (BUTT)");
        sb.AppendLine($"OpPerim = Σ(2×(w+h)×count) = {opPerim} ft");
        sb.AppendLine();

        var jMul = input.Trims.CeilingTransition is "crown-base" or "cove" or "f-trim" ? 1 : 3;
        var jLF = jMul * P + opPerim;
        sb.AppendLine("J-Trim");
        sb.AppendLine($"J = {jMul}×P + OpPerim = {jMul}×{P} + {opPerim} = {jLF} LF");
        sb.AppendLine();

        sb.AppendLine("Panels");
        sb.AppendLine($"Extras % = {input.ExtraPercent ?? CalcSettings.DefaultExtraPercent}");
        sb.AppendLine($"Base = {result.Panels.BasePanels}");
        sb.AppendLine($"Rounded = {result.Panels.RoundedPanels} (Overage = {result.Panels.OveragePercentRounded:N1}%)");

        CalculationsText = sb.ToString();
        OnPropertyChanged(nameof(CalculationsText));
    }
}

