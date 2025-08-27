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
using Room = Nuform.Core.LegacyCompat.Room;

namespace Nuform.App
{
    public partial class IntakePage : Page
    {
        public static double[] PanelWidths { get; } = new[] { 12.0, 18.0 };
        public static DomainCeilingOrientation[] CeilingOrientations { get; } =
            (DomainCeilingOrientation[])System.Enum.GetValues(typeof(DomainCeilingOrientation));

        public static DomainNuformColor[] PanelColors { get; } =
            (DomainNuformColor[])System.Enum.GetValues(typeof(DomainNuformColor));

        public static DomainOpeningTreatment[] OpeningTreatments { get; } =
            (DomainOpeningTreatment[])System.Enum.GetValues(typeof(DomainOpeningTreatment));

        private ObservableCollection<Room> Rooms { get; } = new();
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
            // Commit any pending edits so we don't read stale/zero values
            try
            {
                RoomsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                RoomsGrid.CommitEdit(DataGridEditingUnit.Row,  true);
                OpeningsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                OpeningsGrid.CommitEdit(DataGridEditingUnit.Row,  true);
                this.BindingGroup?.CommitEdit();
            }
            catch { /* ignore commit errors */ }


            if (!Rooms.Any())
            {
                MessageBox.Show("Enter at least one room");
                return;
            }

            double.TryParse(ContBox.Text, out var contPerc);
            var room = Rooms.First();

            // Enforce color↔panel-width policy
            if (room.WallPanelColor == DomainNuformColor.Black) room.PanelWidthInches = 18;
            else if (room.WallPanelColor == DomainNuformColor.Tan) room.PanelWidthInches = 12;
            var panelWidthFt = room.PanelWidthInches == 18 ? 1.5 : 1.0;

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
                CeilingOrientation = room.CeilingOrientation,
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
