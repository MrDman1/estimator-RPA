using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Nuform.Core.Domain;

namespace Nuform.App;

public partial class ResultsPage : Page, INotifyPropertyChanged
{
    private BuildingInput _input;
    private CalcEstimateResult _result;
    private double _perimeter, _wallArea, _openingsArea, _openingsPerimeterLF;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public string PerimeterFormula => _input.Mode == "ROOM"
        ? $"P = 2(L+W) = 2({_input.Length}+{_input.Width}) = {_perimeter}"
        : $"P = L = {_perimeter}";

    public string PanelFormula =>
        $"Base panels = ceil(({_wallArea - _openingsArea}) / ({_input.PanelCoverageWidthFt}\u00D7{_input.Height})) = {_result.Panels.BasePanels}";

    public string OverageFormula =>
        $"Rounded panels = {_result.Panels.RoundedPanels} (Overage {_result.Panels.OveragePercentRounded:F1}%){(_result.Panels.WarnExceedsConfigured ? " WARN" : "" )}";

    public string JTrimFormula => _input.Trims.JTrimEnabled
        ? $"J-Trim LF = {(_input.Trims.CeilingTransition != null ? 1 : 3)}\u00D7{_perimeter} + {_openingsPerimeterLF} = {_result.Trims.JTrimLF}"
        : "J-Trim disabled";

    public string CeilingFormula => _input.Trims.CeilingTransition != null
        ? $"Ceiling Trim LF = {_result.Trims.CeilingTrimLF}"
        : "";

    public string InsideCornersFormula => $"Inside Corners = {_result.InsideCorners}";

    public ResultsPage(BuildingInput input, CalcEstimateResult result)
    {
        InitializeComponent();
        _input = input;
        _result = result;
        DataContext = this;

        LengthBox.Text = _input.Length.ToString();
        WidthBox.Text = _input.Width.ToString();
        HeightBox.Text = _input.Height.ToString();
        ExtraBox.Text = (_input.ExtraPercent ?? CalcSettings.DefaultExtraPercent).ToString();

        Recalculate();
    }

    private void Recalculate()
    {
        _result = CalcService.CalcEstimate(_input);
        _perimeter = _input.Mode == "ROOM" ? 2 * (_input.Length + _input.Width) : _input.Length;
        _wallArea = _perimeter * _input.Height;
        _openingsArea = 0;
        _openingsPerimeterLF = 0;
        foreach (var op in _input.Openings)
        {
            var area = op.Width * op.Height * op.Count;
            if (op.Treatment == OpeningTreatment.BUTT)
            {
                _openingsArea += area;
                _openingsPerimeterLF += 2 * (op.Width + op.Height) * op.Count;
            }
        }
        OnPropertyChanged(nameof(PerimeterFormula));
        OnPropertyChanged(nameof(PanelFormula));
        OnPropertyChanged(nameof(OverageFormula));
        OnPropertyChanged(nameof(JTrimFormula));
        OnPropertyChanged(nameof(CeilingFormula));
        OnPropertyChanged(nameof(InsideCornersFormula));
    }

    private void InputChanged(object sender, TextChangedEventArgs e)
    {
        double.TryParse(LengthBox.Text, out var l);
        double.TryParse(WidthBox.Text, out var w);
        double.TryParse(HeightBox.Text, out var h);
        double.TryParse(ExtraBox.Text, out var extra);
        _input.Length = l;
        _input.Width = w;
        _input.Height = h;
        _input.ExtraPercent = extra;
        Recalculate();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
        => NavigationService?.GoBack();
}

