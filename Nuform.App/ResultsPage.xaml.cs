using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Nuform.Core;

namespace Nuform.App;

public partial class ResultsPage : Page
{
    private readonly AppState _state;
    private readonly EstimateResult _result;
    private readonly TrimCalculator.TrimResult _trims;

    public ResultsPage()
    {
        InitializeComponent();
        _state = (AppState)Application.Current.FindResource("AppState");
        RoomsSummary.ItemsSource = _state.Rooms;
        OpeningsSummary.ItemsSource = _state.Openings;

        var estimator = new Estimator();
        var input = new EstimateInput
        {
            Rooms = _state.Rooms.ToList(),
            Openings = _state.Openings.ToList(),
            Options = new EstimateOptions { Contingency = (double)_state.ContingencyPercent / 100.0 }
        };
        _result = estimator.Estimate(input);
        WallPanelsList.ItemsSource = _result.WallPanels.Select(kvp => $"{kvp.Value} x {kvp.Key}'");
        CeilingPanelsList.ItemsSource = _result.CeilingPanels.Select(kvp => $"{kvp.Value} x {kvp.Key}'");
        TrimPartsList.ItemsSource = _result.Parts.Select(p => $"{p.PartCode}: {p.QtyPacks} packs ({p.LFNeeded:F1} LF needed, {p.TotalLFProvided:F1} LF provided)");
        HardwareText.Text = $"Plugs/Spacers: {_result.Hardware.PlugSpacerPacks} packs\nExpansion Tools: {_result.Hardware.ExpansionTools}\nScrews: {_result.Hardware.ScrewBoxes} boxes";

        bool useCeil = _state.Rooms.Any(r => r.HasCeiling);
        _trims = TrimCalculator.Calculate(_state.Rooms, useCeil);
        TrimsSummary.DataContext = _trims;
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        var intake = new IntakePage();
        if (intake.DataContext is IntakeViewModel vm)
        {
            _state.ApplyTo(vm);
        }
        NavigationService?.Navigate(intake);
    }

    private void ResolveFolders_Click(object sender, RoutedEventArgs e)
    {
        var cfg = ConfigService.Load();
        var est = PathDiscovery.FindEstimateFolder(cfg.WipEstimatingRoot, _state.EstimateNumber);
        var bom = PathDiscovery.FindBomFolder(cfg.WipDesignRoot, _state.EstimateNumber);
        EstimatePathText.Text = est ?? "Estimate folder not found";
        BomPathText.Text = bom ?? "BOM folder not found";
    }

    private void GenerateSof_Click(object sender, RoutedEventArgs e)
    {
        var cfg = ConfigService.Load();
        var catalog = new CatalogService(); // should already load parts.csv or PDF-backed data

        // TODO: replace with the real BOM number the user selected, e.g., from a textbox
        var bomNumber = "135079-01";
        var bomCurrent = PathDiscovery.FindBomFolder(cfg.WipDesignRoot, bomNumber);
        if (string.IsNullOrEmpty(bomCurrent))
        {
            MessageBox.Show("BOM 1-CURRENT not found. Create the BOM in NSD, then try again.");
            return;
        }

        var target = Path.Combine(bomCurrent, $"{bomNumber}.sof");
        var header = new SofHeader { Date = DateTime.Today, ShipTo = "SoldTo", FreightBy = "Nuform" };
        try
        {
            SofWriter.Write(target, _result, catalog, header, panelColor: "BRIGHT WHITE", panelWidthInches: 18);
            MessageBox.Show($"SOF created:\n{target}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"SOF not created:\n{ex.Message}");
        }
    }

    private void FillPrint_Click(object sender, RoutedEventArgs e)
    {
        var cfg = ConfigService.Load();
        var est = PathDiscovery.FindEstimateFolder(cfg.WipEstimatingRoot, _state.EstimateNumber);
        if (est == null) { MessageBox.Show("Estimate folder not found"); return; }
        try
        {
            var pdf = ExcelService.FillAndPrint(cfg, _state.EstimateNumber, _result, Path.Combine(est, "ESTIMATE"), _trims);
            PdfPathText.Text = pdf;
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{pdf}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Excel Error");
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Open folder not implemented");
}
