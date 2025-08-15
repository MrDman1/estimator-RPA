using System.Collections.ObjectModel;

namespace Nuform.Core;

public class AppState
{
    public string EstimateNumber { get; set; } = "";
    public decimal ContingencyPercent { get; set; }
    public ObservableCollection<Room> Rooms { get; } = new();
    public ObservableCollection<Opening> Openings { get; } = new();

    public void LoadFrom(IntakeViewModel vm)
    {
        EstimateNumber = vm.EstimateNumber;
        ContingencyPercent = vm.ContingencyPercent;
        Rooms.Clear();
        foreach (var r in vm.Rooms)
            Rooms.Add(r);
        Openings.Clear();
        foreach (var o in vm.Openings)
            Openings.Add(o);
    }

    public void ApplyTo(IntakeViewModel vm)
    {
        vm.EstimateNumber = EstimateNumber;
        vm.ContingencyPercent = ContingencyPercent;
        vm.Rooms.Clear();
        foreach (var r in Rooms)
            vm.Rooms.Add(r);
        vm.Openings.Clear();
        foreach (var o in Openings)
            vm.Openings.Add(o);
    }
}
