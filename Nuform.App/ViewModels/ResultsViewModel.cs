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
    /// <summary>
    /// ResultsViewModel with:
    /// - SOF export
    /// - Panel overage visibility (contingency + rounding)
    /// - Proper pack/box conversions for trim/screws
    /// - Unified Overages (% over Base) that include initial contingency and user Change
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
                var vm  = new CalculationsViewModel(State);
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
                            Units    = string.IsNullOrEmpty(row.Unit) ? row.Unit : row.Unit.ToUpperInvariant(),
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

            BackCommand   = new RelayCommand(_ => NavigateBack());
            ResetCommand  = new RelayCommand(_ => ResetToIntake());
            FinishCommand = new RelayCommand(_ => System.Windows.Application.Current.Shutdown());

            State.Updated += Recalculate;
            Recalculate();
        }

        public int     BasePanels            { get; private set; }
        public int     RoundedPanels         { get; private set; }
        public decimal OveragePercentRounded { get; private set; }
        public bool    ShowOverageWarning    { get; private set; }

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
        public ICommand ExportPdfCommand        { get; }
        public ICommand ExportCsvCommand        { get; }
        public ICommand BackCommand             { get; }
        public ICommand ResetCommand            { get; }
        public ICommand FinishCommand           { get; }

        private void Recalculate()
        {
            State.Result = CalcService.CalcEstimate(State.Input);

            BasePanels            = State.Result.Panels.BasePanels;
            RoundedPanels         = State.Result.Panels.RoundedPanels;
            OveragePercentRounded = (decimal)State.Result.Panels.OveragePercentRounded;
            ShowOverageWarning    = State.Result.Panels.WarnExceedsConfigured;

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
                // Normalize to UI category
                string normCat = NormalizeCategory(item.Category);

                // Default: assume Suggested is what BOM gave us
                decimal suggested = item.Quantity;

                // We'll compute BaseQtyUnits in SAME UNITS as suggested.
                // For Panels it's clear (panels). For Trim/Screws we convert LF/pieces overage to packs/boxes.
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
                    PartNumber          = item.PartNumber,
                    Name                = item.Name,
                    SuggestedQty        = suggested,
                    BaseQtyUnits        = baseQtyUnits,
                    InitialOverageUnits = initialOverageUnits,
                    Unit                = item.Unit,
                    Category            = normCat,
                    PackSize            = packSize,
                    PackBasis           = packBasis,
                    Change              = "0"
                });
            }

            OnPropertyChanged(nameof(BillOfMaterials));
        }

        /// <summary>
        /// For each item, determine pack/box sizing (catalog-aware), convert BOM "overage"
        /// to SAME UNITS as Suggested, and compute Base and InitialOverage.
        /// </summary>
        private void ComputePackagingAndBase(
            string normCat,
            dynamic item, // BOM service item (has Quantity, Overage, Category, Name, PartNumber, Unit)
            out decimal? packSize,
            out string packBasis,
            out decimal baseQtyUnits,
            out decimal initialOverageUnits,
            ref decimal suggested)
        {
            packSize  = null;
            packBasis = string.Empty;

            if (normCat == "Panels")
            {
                // Panels: Suggested should reflect RoundedPanels; Base is BasePanels.
                suggested            = RoundedPanels;
                baseQtyUnits         = BasePanels;
                initialOverageUnits  = suggested - baseQtyUnits; // shows contingency + rounding
                return;
            }

            // Non-panels (Trim/Other/Accessories): try to pull pack metadata from Catalog.
            // We support both catalog-derived sizes and robust fallbacks.
            if (!TryGetCatalogPackaging(item, out packSize, out packBasis))
            {
                // Heuristics / fallbacks (kept conservative)
                if (normCat == "Trim")
                {
                    packSize  = 160m; // e.g., 10 sticks × 16' → 160 LF per pack
                    packBasis = "LF";
                }
                else if (normCat == "Other" &&
                         item.Name is string n &&
                         n.IndexOf("screw", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    packSize  = 500m; // 500 pcs per box
                    packBasis = "pcs";
                }
                else
                {
                    // No conversion context; treat overage as same units as Suggested.
                    packSize  = null;
                    packBasis = string.Empty;
                }
            }

            // Convert BOM overage (usually LF or pcs) to packs/boxes if we know pack size.
            // item.Overage is assumed to be "native units" (LF for trim; pcs for screws; etc).
            decimal overageNative = item.Overage;

            if (packSize.HasValue && packSize.Value > 0m && !string.IsNullOrEmpty(packBasis))
            {
                // Suggested here is ALREADY in packs/boxes (from BOM).
                // Base = Suggested - (overageNative / packSize)
                baseQtyUnits        = suggested - (overageNative / packSize.Value);
                initialOverageUnits = suggested - baseQtyUnits;
            }
            else
            {
                // Fall back: assume overage is in same units as Suggested.
                baseQtyUnits        = suggested - overageNative;
                initialOverageUnits = overageNative;
            }

            // Guard against negative base (can happen with rounding)
            if (baseQtyUnits < 0m) baseQtyUnits = 0m;
        }

        /// <summary>
        /// Attempts to read packaging info from the catalog. If your CatalogService
        /// exposes richer metadata, map it here. This version looks for common fields
        /// and falls back gracefully.
        /// </summary>
        private bool TryGetCatalogPackaging(dynamic item, out decimal? packSize, out string packBasis)
        {
            packSize  = null;
            packBasis = string.Empty;

            try
            {
                // Prefer an explicit catalog lookup if available.
                var cat = _catalog.Get(item.PartNumber); // Adjust if your CatalogService uses a different API

                // Example mappings — adapt these to your actual catalog model:
                // - cat.PackLf      : decimal? (LF per pack)
                // - cat.PiecesPerBox: int?
                // - cat.Unit        : "pkg"/"pcs"/...

                // LF-based packs (trim, moldings)
                if (cat?.PackLf is decimal lf && lf > 0m)
                {
                    packSize  = lf;
                    packBasis = "LF";
                    return true;
                }

                // Piece-based boxes (screws, fasteners)
                if (cat?.PiecesPerBox is int ppb && ppb > 0)
                {
                    packSize  = ppb;
                    packBasis = "pcs";
                    return true;
                }

                // Some catalogs carry stick-length × count (e.g., 10 × 16ft)
                if (cat?.StickLengthFt is decimal stickFt && stickFt > 0m &&
                    cat?.SticksPerPack is int sticks && sticks > 0)
                {
                    packSize  = stickFt * sticks; // LF per pack
                    packBasis = "LF";
                    return true;
                }
            }
            catch
            {
                // Catalog lookup failed or API not available; fall through to false.
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

        // === INotifyPropertyChanged ===
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Map detailed BOM categories to UI buckets.
        /// </summary>
        private static string NormalizeCategory(string cat)
        {
            return cat switch
            {
                "Panels"      => "Panels",
                "Accessories" => "Accessories",
                "Screws"      => "Other", // screws under Other per your UI
                _             => "Trim",
            };
        }
    }
}
