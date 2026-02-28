using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ProcessesMonitor.Models;

public class ProcessInfo : INotifyPropertyChanged
{
    private ProcessPriorityClass _priority;
    private long _memoryUsage;
    private int _threadCount;
    private TimeSpan _cpuTime;
    private IntPtr _processorAffinity;
    private ObservableCollection<ThreadInfo> _threads = new();

    public int Id { get; set; }
    public string Name { get; set; }
    public string FullName { get; set; }
    
    public ProcessPriorityClass Priority
    {
        get => _priority;
        set
        {
            _priority = value;
            OnPropertyChanged(nameof(Priority));
        }
    }
    
    public long MemoryUsage
    {
        get => _memoryUsage;
        set 
        { 
            _memoryUsage = value; 
            OnPropertyChanged(nameof(MemoryUsage)); 
            OnPropertyChanged(nameof(MemoryUsageFormatted)); 
        }
    }
    
    public int ThreadCount
    {
        get => _threadCount;
        set
        {
            _threadCount = value;
            OnPropertyChanged(nameof(ThreadCount));
        }
    }
    
    public TimeSpan CpuTime
    {
        get => _cpuTime;
        set { 
            _cpuTime = value; 
            OnPropertyChanged(nameof(CpuTime)); 
            OnPropertyChanged(nameof(CpuTimeFormatted)); 
        }
    }
    
    public int ParentId { get; set; }
    
    public IntPtr ProcessorAffinity
    {
        get => _processorAffinity;
        set
        {
            _processorAffinity = value; 
            OnPropertyChanged(nameof(ProcessorAffinity)); 
            OnPropertyChanged(nameof(AffinityHex)); 
            OnPropertyChanged(nameof(AffinityBinary));
        }
    }
    
    public ObservableCollection<ThreadInfo> Threads
    {
        get => _threads;
        set
        {
            _threads = value;
            OnPropertyChanged(nameof(Threads));
        }
    }
    
    // Вычисляемые свойства для UI
    public string MemoryUsageFormatted => $"{MemoryUsage / 1024 / 1024} MB";
    public string CpuTimeFormatted => $"{CpuTime.TotalSeconds:F2} s";
    public string AffinityHex => $"0x{ProcessorAffinity.ToInt64():X}";
    public string AffinityBinary => Convert.ToString(ProcessorAffinity.ToInt64(), 2).PadLeft(Environment.ProcessorCount, '0');

    // Метод обновления данных из другого объекта (для refresh)
    public void UpdateFrom(ProcessInfo other)
    {
        if (other == null) return;
        Priority = other.Priority;
        MemoryUsage = other.MemoryUsage;
        ThreadCount = other.ThreadCount;
        CpuTime = other.CpuTime;
        ParentId = other.ParentId;
        ProcessorAffinity = other.ProcessorAffinity;
        FullName = other.FullName;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}