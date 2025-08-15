using System.Collections.Generic;
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
        TrimsText.Text = $"J-Trim: {result.Trims.JTrimLF:F1} LF ({result.Trims.JTrimPacks} packs)\nCorner Trim: {result.Trims.CornerTrimLF:F1} LF ({result.Trims.CornerPacks} packs)\nTop Track: {result.Trims.TopTrackLF:F1} LF";
        HardwareText.Text = $"Plugs/Spacers: {result.Hardware.PlugSpacerPacks} packs\nExpansion Tools: {result.Hardware.ExpansionTools}\nScrews: {result.Hardware.ScrewBoxes} boxes";
    }

    private void ResolveFolders_Click(object sender, RoutedEventArgs e)
    {
        var cfg = ConfigService.Load();
        var est = PathDiscovery.FindEstimateFolder(cfg.WipEstimatingRoot, _estimateNumber);
        EstimatePathText.Text = est ?? "Estimate folder not found";
    }

    private void GenerateSof_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show(".SOF generation not implemented");

    private void FillPrint_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Excel generation not implemented");

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Open folder not implemented");
}
