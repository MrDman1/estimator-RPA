using System.Windows.Controls;
using Nuform.App.ViewModels;
using VmEstimateState = Nuform.App.ViewModels.EstimateState;

namespace Nuform.App.Views
{
    public partial class ResultsPage : Page
    {
        public ResultsPage(VmEstimateState state)
        {
            InitializeComponent();
            DataContext = new ResultsViewModel(state);
        }
    }
}
