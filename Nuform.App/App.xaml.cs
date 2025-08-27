using System.Windows;

namespace Nuform.App;

public partial class App : Application
{
    public App()
    {
        this.DispatcherUnhandledException += (s, e) =>
        {
            System.Windows.MessageBox.Show(
                "A calculation error occurred: " + e.Exception.Message,
                "Estimator", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            e.Handled = true; // keep app responsive
        };
    }

}