using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Nuform.Core.Domain;
using Nuform.App.ViewModels;
using VmEstimateState = Nuform.App.ViewModels.EstimateState;
using DomainCeilingOrientation = Nuform.Core.Domain.CeilingOrientation;
using DomainOpeningTreatment   = Nuform.Core.Domain.OpeningTreatment;
using DomainNuformColor        = Nuform.Core.Domain.NuformColor;

namespace Nuform.App
{
    public class RoomInput
    {
        public double LengthFt { get; set; }
        public double WidthFt { get; set; }
        public double HeightFt { get; set; }
        public double WallPanelLengthFt { get; set; }
        public double PanelWidthInches { get; set; } = 12;
        public bool HasCeiling { get; set; }
        public double CeilingPanelLengthFt { get; set; }
        public DomainCeilingOrientation CeilingOrientation { get; set; } = DomainCeilingOrientation.Lengthwise;
        public DomainNuformColor WallPanelColor { get; set; } = DomainNuformColor.NuformWhite;
        public DomainNuformColor CeilingPanelColor { get; set; } = DomainNuformColor.NuformWhite;
    }

    public partial class IntakePage : Page
    {
        public static double[] PanelWidths { get; } = new[] { 12.0, 18.0 };
        public static DomainCeilingOrientation[] CeilingOrientations { get; } =
            (DomainCeilingOrientation[])System.Enum.GetValues(typeof(DomainCeilingOrientation));

        public static DomainNuformColor[] PanelColors { get; } =
            (DomainNuformColor[])System.Enum.GetValues(typeof(DomainNuformColor));

        public static DomainOpeningTreatment[] OpeningTreatments { get; } =
            (DomainOpeningTreatment[])System.Enum.GetValues(typeof(DomainOpeningTreatment));

        private ObservableCollection<RoomInput> Rooms { get; } = new();
        private ObservableCollection<OpeningInput> Openings { get; } = new();
        private readonly VmEstimateState _state = new();

        public IntakePage()
        {
            InitializeComponent();
            RoomsGrid.ItemsSource = Rooms;
            OpeningsGrid.ItemsSource = Openings;
            DataContext = _state;
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            RoomsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            RoomsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            OpeningsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            OpeningsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            BindingGroup?.CommitEdit();

            if (!Rooms.Any())
            {
                MessageBox.Show("Enter at least one room");
                return;
            }

            double.TryParse(ContBox.Text, out var contPerc);
            var room = Rooms.First();
            var panelWidthFt = room.PanelWidthInches / 12.0;

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
                },
                IncludeCeilingPanels = room.HasCeiling,
                WallPanelSeries = "R3",
                WallPanelWidthInches = (int)room.PanelWidthInches,
                WallPanelLengthFt = (decimal)room.WallPanelLengthFt,
                WallPanelColor = PanelCodeResolver.ColorName(room.WallPanelColor),
                CeilingPanelSeries = "R3",
                CeilingPanelWidthInches = (int)room.PanelWidthInches,
                CeilingPanelLengthFt = (decimal)room.CeilingPanelLengthFt,
                CeilingPanelColor = PanelCodeResolver.ColorName(room.CeilingPanelColor),
                IncludeWallScrews = _state.Input.IncludeWallScrews,
                IncludeCeilingScrews = _state.Input.IncludeCeilingScrews,
                IncludePlugs = _state.Input.IncludePlugs,
                IncludeSpacers = _state.Input.IncludeSpacers,
                IncludeExpansionTool = _state.Input.IncludeExpansionTool
            };

            _state.Input = input;
            _state.Result = CalcService.CalcEstimate(input);

            // Navigate to results page
            NavigationService?.Navigate(new Nuform.App.Views.ResultsPage(_state));
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
}
