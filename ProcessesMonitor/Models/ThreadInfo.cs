using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ProcessesMonitor.Models;

public class ThreadInfo : INotifyPropertyChanged
{
    public int Id { get; set; }
    
    private ThreadPriorityLevel _priority;
    private ThreadState _state;
    private TimeSpan _cpuTime;
    
    public ThreadPriorityLevel Priority
    {
        get => _priority;
        set
        {
            _priority = value;
            OnPropertyChanged(nameof(Priority));
            OnPropertyChanged(nameof(PriorityDisplay));
        }
    }
    
    public ThreadState State
    {
        get => _state;
        set
        {
            _state = value;
            OnPropertyChanged(nameof(State));
            OnPropertyChanged(nameof(StateDisplay));
        }
    }
    
    public TimeSpan CpuTime
    {
        get => _cpuTime;
        set
        {
            _cpuTime = value;
            OnPropertyChanged(nameof(CpuTime));
        }
    }
    
    public string StateDisplay => State.ToString();
    public string PriorityDisplay => Priority.ToString();
    
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}