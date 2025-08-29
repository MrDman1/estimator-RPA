using System.Windows;

namespace Nuform.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Navigate to the automation agent landing page on startup.
        // From there the user can choose to run the existing calculator
        // (RELINE), automate data entry based off an uploaded file, or run
        // other automation workflows as they are added.
        MainFrame.Navigate(new Pages.AgentPage());
    }
}
