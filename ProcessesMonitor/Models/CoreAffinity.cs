using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProcessesMonitor.Models;

public class CoreAffinity : INotifyPropertyChanged
{
    public int CoreIndex { get; set; }
    
    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            OnPropertyChanged(nameof(IsEnabled));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}