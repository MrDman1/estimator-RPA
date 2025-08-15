using System.Collections.ObjectModel;

namespace Nuform.Core;

public class IntakeViewModel
{
    public string EstimateNumber { get; set; } = "";
    public decimal ContingencyPercent { get; set; }
    public ObservableCollection<Room> Rooms { get; } = new();
    public ObservableCollection<Opening> Openings { get; } = new();
}
