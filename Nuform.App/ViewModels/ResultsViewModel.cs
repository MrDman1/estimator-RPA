using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Nuform.Core.Domain;
using Nuform.Core.Services;
using Nuform.App.Models;
using Nuform.App.Services;
using Nuform.App.Views;

namespace Nuform.App.ViewModels
{
    public class ResultsViewModel : INotifyPropertyChanged
    {
        private readonly EstimateState _state;
        private readonly CatalogService _catalog = new();
        private bool _catalogError;

        private decimal _extrasPercent;
        private string _extrasPercentText = "5";

        public ResultsViewModel(EstimateState state)
        {
            _state = state;

            _extrasPercent = (decimal?)(state.Input.ExtraPercent) ?? (decimal)CalcSettings.DefaultExtraPercent;
            state.Input.ExtraPercent = (double)_extrasPercent;
            _extrasPercentText = _extrasPercent.ToString();

            OpenCalculationsCommand = new RelayCommand(_ =>
            {
                var vm = new CalculationsViewModel(_state);
                var win = new CalculationsWindow { DataContext = vm };
                win.Owner = Application.Current.MainWindow;
                win.Show();
            });

            ExportPdfCommand = new RelayCommand(_ =>
            {
                var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                        $"estimate_parts_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
                PdfExportService.ExportBom(file, BillOfMaterials, "Estimate Results");
                MessageBox.Show($"PDF saved:\n{file}", "Export PDF");
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

            FinishCommand = new RelayCommand(_ => { });

            _state.Updated += Recalculate;
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

            var bom = BomService.Build(_state.Input, _state.Result, _catalog, out var missing);
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
