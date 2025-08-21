using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Nuform.Core;
using Nuform.Core.Domain;
using Nuform.App.Views;
using Nuform.App.ViewModels;

namespace Nuform.App;

public partial class IntakePage : Page
{
    public static double[] PanelWidths { get; } = new[] { 12.0, 18.0 };
    public static CeilingOrientation[] CeilingOrientations { get; } =
        (CeilingOrientation[])System.Enum.GetValues(typeof(CeilingOrientation));

    public static OpeningTreatment[] OpeningTreatments { get; } =
        (OpeningTreatment[])System.Enum.GetValues(typeof(OpeningTreatment));

    private ObservableCollection<Room> Rooms { get; } = new();
    private ObservableCollection<OpeningInput> Openings { get; } = new();

    public IntakePage()
    {
        InitializeComponent();
        RoomsGrid.ItemsSource = Rooms;
        OpeningsGrid.ItemsSource = Openings;
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (!Rooms.Any())
        {
            MessageBox.Show("Enter at least one room");
            return;
        }
        double.TryParse(ContBox.Text, out var contPerc);
        var room = Rooms.First();
        var panelWidthFt = room.PanelWidthInches == 18 ? 1.5 : 1.0;
        var input = new BuildingInput
        {
            Mode = "ROOM",
            Length = room.LengthFt,
            Width = room.WidthFt,
            Height = room.HeightFt,
            PanelCoverageWidthFt = panelWidthFt,
            Openings = Openings.ToList(),
            ExtraPercent = contPerc,
            Trims = new TrimSelections
            {
                JTrimEnabled = JTrimCheckBox.IsChecked == true,
                CeilingTransition = GetSelectedTransition()
            }
        };
        var result = CalcService.CalcEstimate(input);
        var state = new EstimateState(input, result);
        NavigationService?.Navigate(new ResultsPage(state));
    }

    private string? GetSelectedTransition()
    {
        foreach (var child in CeilingTransitionPanel.Children)
        {
            if (child is RadioButton rb && rb.GroupName == "CeilingTransition" && rb.IsChecked == true)
            {
                var tag = rb.Tag as string;
                return string.IsNullOrWhiteSpace(tag) ? null : tag;
            }
        }
        return null;
    }
}
