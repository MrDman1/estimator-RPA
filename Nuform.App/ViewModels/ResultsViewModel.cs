using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Nuform.App.ViewModels
{
    public sealed class ResultsViewModel : INotifyPropertyChanged
    {
        public EstimateState State { get; }
        private readonly CatalogService _catalog = new(); // used by BomService.Build
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
                            Quantity = (int)Math.Round(row.FinalQty, MidpointRounding.AwayFromZero),
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

            BackCommand = new RelayCommand(_ => NavigateBack());
            ResetCommand = new RelayCommand(_ => ResetToIntake());
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
                if (_extrasPercent == value) return;
                _extrasPercent = value;
                State.Input.ExtraPercent = (double)value;
                _extrasPercentText = value.ToString();
                OnPropertyChanged(nameof(ExtrasPercent));
                OnPropertyChanged(nameof(ExtrasPercentText));
                Recalculate();
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
            _catalogError = missing;
            OnPropertyChanged(nameof(CatalogError));

            BillOfMaterials.Clear();

            foreach (var item in bom)
            {
                string normCat = NormalizeCategory(item.Category);
                decimal suggested = item.Quantity; // what the BOM recommends for THIS row

                ComputePackagingAndBase(
                    normCat,
                    item,
                    out var packSize,
                    out var packBasis,
                    out var baseQtyUnits,
                    out var initialOverageUnits,
                    ref suggested
                );

                BillOfMaterials.Add(new BomRow
                {
                    PartNumber = item.PartNumber,
                    Name = item.Name,
                    SuggestedQty = suggested,
                    BaseQtyUnits = baseQtyUnits,
                    InitialOverageUnits = initialOverageUnits,
                    Unit = item.Unit,
                    Category = normCat,
                    PackSize = packSize,
                    PackBasis = packBasis,
                    Change = "0"
                });
            }

            OnPropertyChanged(nameof(BillOfMaterials));
        }

        /// <summary>
        /// Compute Base and InitialOverage in the SAME UNITS as 'suggested' for this row.
        /// Panels: if per-row overage not provided, invert Extras% to expose the hidden bump.
        /// Trim/Other: convert native overage (LF/pcs) to packs/boxes using metadata or defaults.
        /// </summary>
        private void ComputePackagingAndBase(
            string normCat,
            dynamic item, // expected fields: Quantity, Overage, Category, Name, PartNumber, Unit (+ optional packaging fields)
            out decimal? packSize,
            out string packBasis,
            out decimal baseQtyUnits,
            out decimal initialOverageUnits,
            ref decimal suggested)
        {
            packSize = null;
            packBasis = string.Empty;

            if (normCat == "Panels")
            {
                // Prefer a per-row overage if the BOM supplies it
                decimal perRowOverage = 0m;
                try { perRowOverage = (decimal)item.Overage; } catch { /* ignore */ }

                if (perRowOverage > 0m)
                {
                    baseQtyUnits = suggested - perRowOverage;
                    if (baseQtyUnits < 0m) baseQtyUnits = 0m;
                    initialOverageUnits = suggested - baseQtyUnits; // equals perRowOverage
                }
                else
                {
                    // BOM did not provide per-row panel overage → derive from Extras%
                    var factor = 1m + (decimal)State.Input.ExtraPercent / 100m;
                    if (factor <= 0m) factor = 1m;

                    // Approximate pre-extras base; round to whole panels.
                    baseQtyUnits = Math.Round(suggested / factor, 0, MidpointRounding.AwayFromZero);
                    if (baseQtyUnits < 0m) baseQtyUnits = 0m;

                    initialOverageUnits = suggested - baseQtyUnits; // shows the hidden contingency/rounding
                }
                return;
            }

            // === Non-panels: Trim / Other / Accessories ===
            if (!TryGetCatalogPackagingFromItem(item, out packSize, out packBasis))
            {
                if (normCat == "Trim")
                {
                    packSize = 160m; // 10 sticks × 16' = 160 LF/pack (fallback)
                    packBasis = "LF";
                }
                else if (normCat == "Other" &&
                         item.Name is string n &&
                         n.IndexOf("screw", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    packSize = 500m; // 500 pcs/box (fallback)
                    packBasis = "pcs";
                }
                else
                {
                    packSize = null;
                    packBasis = string.Empty;
                }
            }

            decimal overageNative = 0m;
            try { overageNative = (decimal)item.Overage; } catch { /* ignore */ }

            if (packSize.HasValue && packSize.Value > 0m && !string.IsNullOrEmpty(packBasis))
            {
                // Suggested is in packs/boxes; convert native (LF/pcs) to packs/boxes
                baseQtyUnits = suggested - (overageNative / packSize.Value);
                initialOverageUnits = suggested - baseQtyUnits;
            }
            else
            {
                // No packaging info; assume overage is in the same units
                baseQtyUnits = suggested - overageNative;
                initialOverageUnits = overageNative;
            }

            if (baseQtyUnits < 0m) baseQtyUnits = 0m;
        }

        /// <summary>
        /// Try to read packaging info directly from the BOM item.
        /// Recognized fields:
        ///   - PackLf (decimal): LF per pack (trim)
        ///   - PiecesPerBox (int): pieces per box (screws)
        ///   - StickLengthFt (decimal) + SticksPerPack (int): derive LF/pack
        /// </summary>
        private static bool TryGetCatalogPackagingFromItem(
            dynamic item,
            out decimal? packSize,
            out string packBasis)
        {
            packSize = null;
            packBasis = string.Empty;

            static bool TryGetProp<T>(object obj, string name, out T value)
            {
                value = default!;
                var type = obj?.GetType();
                if (type == null) return false;
                var prop = type.GetProperty(name);
                if (prop == null) return false;

                var raw = prop.GetValue(obj);
                if (raw is T t) { value = t; return true; }

                try
                {
                    if (raw != null)
                    {
                        value = (T)Convert.ChangeType(raw, typeof(T));
                        return true;
                    }
                }
                catch { /* ignore */ }

                return false;
            }

            if (TryGetProp<decimal>(item, "PackLf", out var lf) && lf > 0m)
            {
                packSize = lf;
                packBasis = "LF";
                return true;
            }

            if (TryGetProp<int>(item, "PiecesPerBox", out var ppb) && ppb > 0)
            {
                packSize = ppb;
                packBasis = "pcs";
                return true;
            }

            var haveStickLen = TryGetProp<decimal>(item, "StickLengthFt", out var stickFt) && stickFt > 0m;
            var haveStickCount = TryGetProp<int>(item, "SticksPerPack", out var sticks) && sticks > 0;
            if (haveStickLen && haveStickCount)
            {
                packSize = stickFt * sticks; // LF per pack
                packBasis = "LF";
                return true;
            }

            return false;
        }

        private void NavigateBack()
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
        }

        private void ResetToIntake()
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
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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