using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Nuform.Core.Domain;
using Nuform.Core.Services;
using Nuform.App.Models;
using Nuform.App.Services;
using Nuform.App.Views;
using VmEstimateState = Nuform.App.ViewModels.EstimateState;
using ServicesCatalogService = Nuform.Core.Services.CatalogService;

namespace Nuform.App.ViewModels
{
    public sealed class ResultsViewModel : INotifyPropertyChanged
    {
        public VmEstimateState State { get; }
        private readonly ServicesCatalogService _catalog = new();
        private bool _catalogError;

        private decimal _extrasPercent;
        private string _extrasPercentText = "5";

        public ResultsViewModel(VmEstimateState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));

            _extrasPercent = (decimal?)(State.Input.ExtraPercent) ?? (decimal)CalcSettings.DefaultExtraPercent;
            State.Input.ExtraPercent = (double)_extrasPercent;
            _extrasPercentText = _extrasPercent.ToString();

            OpenCalculationsCommand = new RelayCommand(_ =>
            {
                var vm = new CalculationsViewModel(State);
                var win = new CalculationsWindow { DataContext = vm };
                win.Owner = Application.Current.MainWindow;
                win.Show();
            });

            ExportPdfCommand = new RelayCommand(_ =>
            {
                var dlg = new SaveFileDialog
                {
                    Title = "Save Estimate PDF",
                    Filter = "PDF|*.pdf",
                    FileName = $"estimate_parts_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                    AddExtension = true,
                    OverwritePrompt = true
                };
                if (dlg.ShowDialog() == true)
                {
                    PdfExportService.ExportBom(dlg.FileName, BillOfMaterials, "Estimate Results");
                    MessageBox.Show($"PDF saved:\n{dlg.FileName}", "Export PDF");
                }
            });

            ExportCsvCommand = new RelayCommand(_ => { });

            BackCommand = new RelayCommand(_ =>
            {
                // Try WPF NavigationService (Frame hosted in MainWindow)
                var mainWindow = System.Windows.Application.Current.MainWindow as Nuform.App.MainWindow;
                var frame = mainWindow?.MainFrame;
                if (frame != null)
                {
                    if (frame.CanGoBack) frame.GoBack();
                    else frame.Navigate(new Nuform.App.IntakePage());
                    return;
                }

                // NavigationWindow path
                if (System.Windows.Application.Current.MainWindow is System.Windows.Navigation.NavigationWindow nav)
                {
                    if (nav.CanGoBack) nav.GoBack();
                    else nav.Navigate(new Nuform.App.IntakePage());
                    return;
                }

                // Fallback: set window content directly
                System.Windows.Application.Current.MainWindow.Content = new Nuform.App.IntakePage();
            });

            ResetCommand = new RelayCommand(_ =>
            {
                State.Reset();
                var intake = new Nuform.App.IntakePage();

                var mainWindow = System.Windows.Application.Current.MainWindow as Nuform.App.MainWindow;
                var frame = mainWindow?.MainFrame;
                if (frame != null)
                {
                    frame.Navigate(intake);
                    return;
                }

                if (System.Windows.Application.Current.MainWindow is System.Windows.Navigation.NavigationWindow nav)
                {
                    nav.Navigate(intake);
                    return;
                }

                System.Windows.Application.Current.MainWindow.Content = intake;
            });

            FinishCommand = new RelayCommand(_ => System.Windows.Application.Current.Shutdown());

            State.Updated += Recalculate;
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
                    State.Input.ExtraPercent = (double)value;
                    _extrasPercentText = value.ToString();
                    OnPropertyChanged(nameof(ExtrasPercent));
                    OnPropertyChanged(nameof(ExtrasPercentText));
                    Recalculate();
                }
            }
        }

        public string ExtrasPercentText
        {
            get => _extrasPercentText;
            set
            {
                if (_extrasPercentText == value) return;
                _extrasPercentText = value;
                if (decimal.TryParse(value, out var v))
                    ExtrasPercent = v;
                OnPropertyChanged(nameof(ExtrasPercentText));
            }
        }

        public ObservableCollection<BomRow> BillOfMaterials { get; } = new();
        public bool CatalogError => _catalogError;

        public ICommand OpenCalculationsCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand FinishCommand { get; }

        private void Recalculate()
        {
            State.Result = CalcService.CalcEstimate(State.Input);
            BasePanels = State.Result.Panels.BasePanels;
            RoundedPanels = State.Result.Panels.RoundedPanels;
            OveragePercentRounded = (decimal)State.Result.Panels.OveragePercentRounded;
            ShowOverageWarning = State.Result.Panels.WarnExceedsConfigured;

            OnPropertyChanged(nameof(BasePanels));
            OnPropertyChanged(nameof(RoundedPanels));
            OnPropertyChanged(nameof(OveragePercentRounded));
            OnPropertyChanged(nameof(ShowOverageWarning));

            var bom = BomService.Build(State.Input, State.Result, _catalog, out var missing);
            BillOfMaterials.Clear();
            foreach (var item in bom)
            {
                BillOfMaterials.Add(new BomRow
                {
                    PartNumber = item.PartNumber,
                    Name = item.Name,
                    SuggestedQty = item.Quantity,
                    Unit = item.Unit,
                    Category = item.Category,
                    Change = "0"
                });
            }
            OnPropertyChanged(nameof(BillOfMaterials));
            _catalogError = missing;
            OnPropertyChanged(nameof(CatalogError));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
