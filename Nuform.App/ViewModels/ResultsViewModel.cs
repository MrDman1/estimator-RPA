using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Nuform.Core.Domain;
using Nuform.App.Views;

namespace Nuform.App.ViewModels;

public class ResultsViewModel : INotifyPropertyChanged
{
    private readonly EstimateState _state;

    private decimal _extrasPercent;

    public ResultsViewModel(EstimateState state)
    {
        _state = state;

        _extrasPercent = (decimal?)(state.Input.ExtraPercent) ?? (decimal)CalcSettings.DefaultExtraPercent;
        state.Input.ExtraPercent = (double)_extrasPercent;

        OpenCalculationsCommand = new RelayCommand(_ =>
        {
            var vm = new CalculationsViewModel(_state);
            var win = new CalculationsWindow { DataContext = vm };
            win.Owner = Application.Current.MainWindow;
            win.Show();
        });
        ExportPdfCommand = new RelayCommand(_ => { });
        ExportCsvCommand = new RelayCommand(_ => { });
        BackCommand = new RelayCommand(_ => { });
        FinishCommand = new RelayCommand(_ => { });

        Recalculate();
    }

    public int BasePanels { get; private set; }
    public int RoundedPanels { get; private set; }
    public decimal OveragePercentRounded { get; private set; }
    public bool ShowOverageWarning { get; private set; }

    public decimal ExtrasPercent
    {
        get => _extrasPercent;
        set
        {
            if (_extrasPercent != value)
            {
                _extrasPercent = value;
                _state.Input.ExtraPercent = (double)value;
                OnPropertyChanged(nameof(ExtrasPercent));
                Recalculate();
            }
        }
    }

    public ObservableCollection<BomLineItem> BillOfMaterials { get; } = new();

    public ICommand OpenCalculationsCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand FinishCommand { get; }

    private void Recalculate()
    {
        _state.Result = CalcService.CalcEstimate(_state.Input);
        BasePanels = _state.Result.Panels.BasePanels;
        RoundedPanels = _state.Result.Panels.RoundedPanels;
        OveragePercentRounded = (decimal)_state.Result.Panels.OveragePercentRounded;
        ShowOverageWarning = _state.Result.Panels.WarnExceedsConfigured;
        OnPropertyChanged(nameof(BasePanels));
        OnPropertyChanged(nameof(RoundedPanels));
        OnPropertyChanged(nameof(OveragePercentRounded));
        OnPropertyChanged(nameof(ShowOverageWarning));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
