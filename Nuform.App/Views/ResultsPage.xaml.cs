using System.Windows.Controls;
using Nuform.App.ViewModels;

// Alias the Core type so there’s no ambiguity
using CoreEstimateState = Nuform.Core.Domain.EstimateState;

namespace Nuform.App.Views
{
    public partial class ResultsPage : Page
    {
        public ResultsPage(CoreEstimateState state)
        {
            InitializeComponent();
            DataContext = new ResultsViewModel(state);
        }
    }
}