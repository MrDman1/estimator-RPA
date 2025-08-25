using System.Windows.Controls;
using Nuform.App.ViewModels;

namespace Nuform.App.Views
{
    public partial class ResultsPage : Page
    {
        public ResultsPage(EstimateState state)
        {
            InitializeComponent();
            DataContext = new ResultsViewModel(state);
        }
    }
}
