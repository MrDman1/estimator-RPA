using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Nuform.Core.Domain;
using CoreEstimateState = Nuform.Core.Domain.EstimateState;

namespace Nuform.App.ViewModels;

public sealed class CalculationsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string? n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    public ObservableCollection<FormulaRow> Rows { get; } = new();

    private readonly CoreEstimateState _state;
    private CalcEstimateResult _last;

    public CalculationsViewModel(CoreEstimateState state)
    {
        _state = state;
        _last = CalcService.CalcEstimate(_state.Input);
        _state.Result = _last;
        BuildRows();
    }

    private void BuildRows()
    {
        Rows.Clear();
        var input = _state.Input;
        var result = _last;

        // Perimeter
        var P = input.Mode == "ROOM"
            ? 2m * ((decimal)input.Length + (decimal)input.Width)
            : (decimal)input.Length;
        Rows.Add(FormulaRow.Const("Perimeter",
            "P = 2(L+W) (room) or P = L (wall)",
            $"P = {(input.Mode=="ROOM" ? $"2×({input.Length}+{input.Width})" : $"{input.Length}")} = {P} ft"));

        // Openings perimeter (BUTT only)
        decimal opPerim = 0m;
        foreach (var op in input.Openings)
            if (op.Treatment == OpeningTreatment.BUTT)
                opPerim += 2m * ((decimal)op.Width + (decimal)op.Height) * op.Count;
        Rows.Add(FormulaRow.Const("Openings Perimeter (BUTT)",
            "OpPerim = Σ 2(w+h)×count",
            $"OpPerim = {opPerim} ft"));

        // J-Trim rule
        var ceilingSelected = input.Trims.CeilingTransition is "crown-base" or "cove" or "f-trim";
        var jMul = ceilingSelected ? 1 : 3;
        var jLF = jMul * P + opPerim;
        Rows.Add(FormulaRow.Const("J-Trim",
            ceilingSelected ? "J = 1×P + OpPerim (ceiling trim selected)" : "J = 3×P + OpPerim",
            $"J = {jMul}×{P} + {opPerim} = {jLF} LF"));

        // Ceiling transition LF
        if (ceilingSelected)
            Rows.Add(FormulaRow.Const("Ceiling Trim",
                "CeilingTrim = P",
                $"CeilingTrim = {P} LF"));

        // Inside corners
        int insideCorners = CalcService.ComputeInsideCorners(input);
        var insideLF = insideCorners * (decimal)input.Height;
        Rows.Add(FormulaRow.Const("Inside Corners",
            "InsideCorners = 4 (room L>1 & W>1) or 0 (single wall); LF = count × H",
            $"InsideCorners = {insideCorners}, LF = {insideCorners}×{input.Height} = {insideLF} LF"));

        // Panels
        Rows.Add(FormulaRow.Editable("Panels",
            new [] {
                new Var("Extras %", (decimal)(input.ExtraPercent ?? CalcSettings.DefaultExtraPercent), v => { input.ExtraPercent = (double)v; Recompute(); })
            },
            $"Base = {result.Panels.BasePanels}",
            $"Rounded = {result.Panels.RoundedPanels}  (Overage = {result.Panels.OveragePercentRounded:N1}%)"));
    }

    private void Recompute()
    {
        _last = CalcService.CalcEstimate(_state.Input);
        _state.Result = _last;
        _state.RaiseUpdated();
        BuildRows();
        OnPropertyChanged(nameof(Rows));
    }
}

public sealed class FormulaRow
{
    public string Title { get; init; } = "";
    public string Expression { get; init; } = "";
    public string Evaluated { get; init; } = "";
    public ObservableCollection<Var> Variables { get; } = new();

    public static FormulaRow Const(string title, string expr, string eval) =>
        new FormulaRow { Title = title, Expression = expr, Evaluated = eval };

    public static FormulaRow Editable(string title, Var[] vars, string expr, string eval)
    {
        var r = new FormulaRow { Title = title, Expression = expr, Evaluated = eval };
        foreach (var v in vars) r.Variables.Add(v);
        return r;
    }
}

public sealed class Var : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string? n=null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    public string Name { get; }
    private decimal _value;
    private readonly Action<decimal> _onChange;

    public Var(string name, decimal value, Action<decimal> onChange)
    {
        Name = name; _value = value; _onChange = onChange;
    }
    public decimal Value
    {
        get => _value;
        set { if (_value==value) return; _value = value; _onChange(value); OnChanged(nameof(Value)); }
    }
}
