using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProcessesMonitor.Models;

public class ProcessNode : INotifyPropertyChanged
{
    public ProcessInfo Process { get; set; }
    
    public ObservableCollection<ProcessNode> Children { get; set; } = new();
    
    public int ChildrenCount => Children.Count;
    
    public void NotifyChildrenCount()
    {
        OnPropertyChanged(nameof(ChildrenCount));
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}