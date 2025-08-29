using System.Windows;
using System.Windows.Controls;

namespace Nuform.App.Pages
{
    /// <summary>
    /// Interaction logic for AgentPage.xaml
    /// </summary>
    public partial class AgentPage : Page
    {
        public AgentPage()
        {
            InitializeComponent();
        }

        // Placeholder for file input automation.  In a future iteration this
        // will navigate to a page that accepts a document (.doc, .sof, etc.)
        // and orchestrates the data entry across supported applications.  For now
        // it simply displays a message to the user.
        private void FileInput_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("File input automation is not yet implemented. This option will allow you to upload a document or .SOF file and automatically perform data entry across supported applications.", 
                "Coming soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Placeholder for calculator‑driven automation.  Future versions will
        // read data from the existing view models and push the results into
        // external systems automatically.  For now it simply informs the user
        // that the feature is under development.
        private void CalcAutomation_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Calculator automation is not yet implemented. This option will run the Nuform calculations and push the resulting BOM into external systems automatically.", 
                "Coming soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Navigate to the existing intake/calculator page.  This preserves all
        // current functionality of the estimator while giving users the
        // ability to choose other automation paths from the landing screen.
        private void Calculator_Click(object sender, RoutedEventArgs e)
        {
            var window = Application.Current.MainWindow as MainWindow;
            if (window != null)
            {
                window.MainFrame.Navigate(new IntakePage());
            }
        }
    }
}