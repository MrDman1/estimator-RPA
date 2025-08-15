using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Nuform.Core;

namespace Nuform.App;

public partial class ResultsPage : Page
{
    private readonly EstimateResult _result;
    private readonly string _estimateNumber;
    public ResultsPage(EstimateResult result, string estimateNumber)
    {
        InitializeComponent();
        _result = result;
        _estimateNumber = estimateNumber;
        WallPanelsList.ItemsSource = result.WallPanels.Select(kvp => $"{kvp.Value} x {kvp.Key}'");
        CeilingPanelsList.ItemsSource = result.CeilingPanels.Select(kvp => $"{kvp.Value} x {kvp.Key}'");
        TrimPartsList.ItemsSource = result.Parts.Select(p => $"{p.PartCode}: {p.QtyPacks} packs ({p.LFNeeded:F1} LF needed, {p.TotalLFProvided:F1} LF provided)");
        HardwareText.Text = $"Plugs/Spacers: {result.Hardware.PlugSpacerPacks} packs\nExpansion Tools: {result.Hardware.ExpansionTools}\nScrews: {result.Hardware.ScrewBoxes} boxes";
    }

    private void ResolveFolders_Click(object sender, RoutedEventArgs e)
    {
        var cfg = ConfigService.Load();
        var est = PathDiscovery.FindEstimateFolder(cfg.WipEstimatingRoot, _estimateNumber);
        var bom = PathDiscovery.FindBomFolder(cfg.WipDesignRoot, _estimateNumber);
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

        var target = System.IO.Path.Combine(bomCurrent, $"{bomNumber}.sof");
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
        var est = PathDiscovery.FindEstimateFolder(cfg.WipEstimatingRoot, _estimateNumber);
        if (est == null) { MessageBox.Show("Estimate folder not found"); return; }
        try
        {
            var pdf = ExcelService.FillAndPrint(cfg, _estimateNumber, _result, Path.Combine(est, "ESTIMATE"));
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
