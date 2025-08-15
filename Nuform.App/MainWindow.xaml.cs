using System.Windows;

namespace Nuform.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainFrame.Navigate(new IntakePage());
    }
}
