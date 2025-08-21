using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Nuform.App.ViewModels;

public class FormulaVariable : INotifyPropertyChanged
{
    private string _value = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class FormulaRow
{
    public string Title { get; set; } = string.Empty;
    public ObservableCollection<FormulaVariable> Variables { get; set; } = new();
    public string Expression { get; set; } = string.Empty;
    public string Evaluated { get; set; } = string.Empty;
}
