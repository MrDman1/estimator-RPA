using System.Windows;
using System.Windows.Controls;
using Nuform.Core;

namespace Nuform.App;

public partial class IntakePage : Page
{
    public static double[] PanelWidths { get; } = new[] { 12.0, 18.0 };
    public static CeilingOrientation[] CeilingOrientations { get; } =
        (CeilingOrientation[])System.Enum.GetValues(typeof(CeilingOrientation));

    public IntakePage()
    {
        InitializeComponent();
        DataContext = new IntakeViewModel();
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not IntakeViewModel vm) return;
        var state = (AppState)Application.Current.FindResource("AppState");
        state.LoadFrom(vm);
        NavigationService?.Navigate(new ResultsPage());
    }
}
