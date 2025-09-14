using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Nuform.Core.Domain;
using Nuform.Core.Services;
using Nuform.App.Models;
using Nuform.App.Services;
using Nuform.App.Views;
using Nuform.Core;

// Import the EstimateState view-model and use direct type names to avoid alias duplication
using Nuform.App.ViewModels;

namespace Nuform.App.ViewModels
{
    /// <summary>
    /// Patched ResultsViewModel implementing SOF export, overage calculation and
    /// category normalization. This version replaces the stubbed ExportCsvCommand
    /// and adds a NormalizeCategory helper. It also computes the overage for
    /// panel lines (rounded minus base panels).
    /// </summary>
    public sealed class ResultsViewModel : INotifyPropertyChanged
    {
        public EstimateState State { get; }
        private readonly CatalogService _catalog = new();
        private bool _catalogError;

        private decimal _extrasPercent;
        private string _extrasPercentText = "5";

        public ResultsViewModel(EstimateState state)
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

            // Implement SOF export using SofV2Writer. Builds parts list from BOM rows.
            ExportCsvCommand = new RelayCommand(_ =>
            {
                var dlg = new SaveFileDialog
                {
                    Title = "Save SOF File",
                    Filter = "SOF|*.sof",
                    FileName = $"estimate_parts_{DateTime.Now:yyyyMMdd_HHmm}.sof",
                    AddExtension = true,
                    OverwritePrompt = true
                };
                if (dlg.ShowDialog() == true)
                {
                    var partsList = new List<SofPart>();
                    foreach (BomRow row in BillOfMaterials)
                    {
                        partsList.Add(new SofPart
                        {
                            PartCode = row.PartNumber,
                            Quantity = (int)row.FinalQty,
                            // Units must be upper-case to align with legacy expectations (e.g. PCS, LF).
                            Units = string.IsNullOrEmpty(row.Unit) ? row.Unit : row.Unit.ToUpperInvariant(),
                            Description = row.Name
                        });
                    }

                    var info = new SofCompanyInfo();
                    try
                    {
                        SofV2Writer.Write(dlg.FileName, info, partsList);
                        MessageBox.Show($"SOF saved:\n{dlg.FileName}", "Export SOF");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to save SOF file:\n{ex.Message}", "Export SOF Error");
                    }
                }
            });

            BackCommand = new RelayCommand(_ =>
            {
                var mainWindow = System.Windows.Application.Current.MainWindow as Nuform.App.MainWindow;
                var frame = mainWindow?.MainFrame;
                if (frame != null)
                {
                    if (frame.CanGoBack) frame.GoBack();
                    else frame.Navigate(new Nuform.App.IntakePage());
                    return;
                }

                if (System.Windows.Application.Current.MainWindow is System.Windows.Navigation.NavigationWindow nav)
                {
                    if (nav.CanGoBack) nav.GoBack();
                    else nav.Navigate(new Nuform.App.IntakePage());
                    return;
                }

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
                string normCat = NormalizeCategory(item.Category);

                // For panel items, compute the overage as the difference between rounded and base panels.
                // For all other items, use the overage value computed by the BOM service (linear
                // footage or piece count converted to a decimal).
                decimal overage = normCat == "Panels"
                    ? RoundedPanels - BasePanels
                    : item.Overage;

                var startDelta = overage; // units
                BillOfMaterials.Add(new BomRow
                {
                    PartNumber = item.PartNumber,
                    Name = item.Name,
                    SuggestedQty = item.Quantity,
                    Unit = item.Unit,
                    Category = normCat,
                    Change = startDelta == 0m ? "0" : (startDelta > 0 ? $"+{startDelta}" : startDelta.ToString())
                });
            }

            OnPropertyChanged(nameof(BillOfMaterials));
            _catalogError = missing;
            OnPropertyChanged(nameof(CatalogError));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Convert the detailed category names returned from the BOM service into the
        /// four high-level categories expected by the UI: Panels, Trim, Accessories
        /// and Other. Panels and Accessories are preserved; screws and hardware
        /// map to Other; all remaining trim kinds map to Trim.
        /// </summary>
        private static string NormalizeCategory(string cat)
        {
            return cat switch
            {
                "Panels" => "Panels",
                "Accessories" => "Accessories",
                "Screws" => "Other",
                _ => "Trim",
            };
        }
    }
}
