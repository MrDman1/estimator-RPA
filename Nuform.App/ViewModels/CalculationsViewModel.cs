using System.Collections.ObjectModel;
using Nuform.Core.Domain;

namespace Nuform.App.ViewModels;

public sealed class CalculationsViewModel
{
    public ObservableCollection<FormulaRow> FormulaRows { get; } = new();

    private readonly EstimateState _state;

    public CalculationsViewModel(EstimateState state)
    {
        _state = state;
        BuildRowsFromState();
        SubscribeToVariableChanges();
    }

    private void BuildRowsFromState()
    {
        FormulaRows.Clear();
        var row = new FormulaRow
        {
            Title = "Panels",
            Expression = $"Base = {_state.Result.Panels.BasePanels}",
            Evaluated = $"Rounded = {_state.Result.Panels.RoundedPanels}"
        };
        row.Variables.Add(new FormulaVariable
        {
            Name = "Extras %",
            Value = _state.Input.ExtraPercent?.ToString() ?? string.Empty
        });
        FormulaRows.Add(row);
    }

    private void SubscribeToVariableChanges()
    {
        foreach (var row in FormulaRows)
        {
            foreach (var v in row.Variables)
            {
                v.PropertyChanged += (_, __) =>
                {
                    if (double.TryParse(v.Value, out var val))
                    {
                        _state.Input.ExtraPercent = val;
                        _state.Result = CalcService.CalcEstimate(_state.Input);
                        BuildRowsFromState();
                    }
                };
            }
        }
    }
}
