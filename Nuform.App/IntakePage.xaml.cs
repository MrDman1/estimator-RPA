using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Nuform.Core;

namespace Nuform.App;

public partial class IntakePage : Page
{
    public static double[] PanelWidths { get; } = new[] { 12.0, 18.0 };
    public static CeilingOrientation[] CeilingOrientations { get; } =
        (CeilingOrientation[])System.Enum.GetValues(typeof(CeilingOrientation));

    private ObservableCollection<Room> Rooms { get; } = new();
    private ObservableCollection<Opening> Openings { get; } = new();

    public IntakePage()
    {
        InitializeComponent();
        RoomsGrid.ItemsSource = Rooms;
        OpeningsGrid.ItemsSource = Openings;
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        double.TryParse(ContBox.Text, out var contPerc);
        var input = new EstimateInput
        {
            Rooms = Rooms.ToList(),
            Openings = Openings.ToList(),
            Options = new EstimateOptions { Contingency = contPerc / 100.0 }
        };
        var estimator = new Estimator();
        var result = estimator.Estimate(input);
        NavigationService?.Navigate(new ResultsPage(result, EstimateNumberBox.Text));
    }
}
