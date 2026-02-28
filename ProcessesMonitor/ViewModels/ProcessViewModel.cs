using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using ProcessesMonitor.Models;
using ProcessesMonitor.Services;
using LiveCharts;
using LiveCharts.Wpf;

namespace ProcessesMonitor.ViewModels;

public class ProcessViewModel : INotifyPropertyChanged
{
    private ObservableCollection<ProcessInfo> _processes = new();
    private ObservableCollection<ProcessNode> _hierarchy = new();
    private ProcessInfo _selectedProcess;
    private string _searchText;
    private bool _showOnlySystem;
    private DispatcherTimer _refreshTimer;
    private Dispatcher _dispatcher;
    private int? _selectedProcessIdBeforeRefresh;
    private int _updateInterval = 2;
    
    public ObservableCollection<ProcessInfo> Processes
    {
        get => _processes;
        set
        {
            _processes = value;
            OnPropertyChanged(nameof(Processes));
        }
    }
    
    public ObservableCollection<ProcessNode> Hierarchy
    {
        get => _hierarchy;
        set
        {
            _hierarchy = value;
            OnPropertyChanged(nameof(Hierarchy));
        }
    }
    
    public ObservableCollection<CoreAffinity> Cores { get; } = new();
    
    public ProcessInfo SelectedProcess
    {
        get => _selectedProcess;
        set 
        { 
            _selectedProcess = value; 
            OnPropertyChanged();
            LoadThreadsForSelected();
            OnPropertyChanged(nameof(CanChangePriority));
            OnPropertyChanged(nameof(CanChangeAffinity));
        }
    }
    
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged(nameof(SearchText));
            ApplyFilters();
        }
    }
    
    public bool ShowOnlySystem
    {
        get => _showOnlySystem;
        set
        {
            _showOnlySystem = value;
            OnPropertyChanged(nameof(ShowOnlySystem));
            ApplyFilters();
        }
    }
    
    public int UpdateInterval
    {
        get => _updateInterval;
        set
        {
            _updateInterval = value;
            OnPropertyChanged();
            if (_refreshTimer != null) _refreshTimer.Interval = TimeSpan.FromSeconds(value);
        }
    }
    
    public int[] AvailableIntervals { get; } = { 2, 5, 10 };
    
    public bool CanChangePriority => SelectedProcess != null;
    public bool CanChangeAffinity => SelectedProcess != null;
    
    // Коллекция для ComboBox приоритетов
    public ProcessPriorityClass[] AvailablePriorities { get; } = Enum.GetValues<ProcessPriorityClass>();
    
    // CPU Cores для чекбоксов
    public int CoreCount => Environment.ProcessorCount;
    
    public bool[] CoreStates { get; private set; }
    
    public SeriesCollection MemoryChartSeries { get; set; } = new SeriesCollection();
    
    public ProcessViewModel(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            Cores.Add(new CoreAffinity { CoreIndex = i, IsEnabled = false });
        }

        SetupCollectionView();
        SetupTimer();
        RefreshProcesses();
    }
    
    private void SetupCollectionView()
    {
        CollectionViewSource.GetDefaultView(Processes).Filter = p =>
        {
            var proc = (ProcessInfo)p;
            
            if (!string.IsNullOrWhiteSpace(SearchText) && 
                !proc.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (ShowOnlySystem && !proc.Name.StartsWith("svchost", StringComparison.OrdinalIgnoreCase)) // Условная системная фильтрация
            {
                return false;
            }
            
            return true;
        };
    }
    
    private void SetupTimer()
    {
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _refreshTimer.Tick += async (s, e) => await RefreshProcessesAsync();
        _refreshTimer.Start();
    }
    
    private async Task RefreshProcessesAsync()
    {
        // Сохранение PID выбранного процесса перед обновлением
        _selectedProcessIdBeforeRefresh = SelectedProcess?.Id;
    
        // Получение свежих данных в фоне
        var freshProcesses = await Task.Run(() => ProcessService.GetAllProcesses());
    
        _dispatcher.Invoke(() =>
        {
            var existingDict = Processes.ToDictionary(p => p.Id);
            var freshDict = freshProcesses.ToDictionary(p => p.Id);
        
            foreach (var id in existingDict.Keys.ToList())
            {
                if (freshDict.ContainsKey(id))
                {
                    // Обновление данных существующего объекта, ссылка не меняется
                    existingDict[id].UpdateFrom(freshDict[id]);
                    existingDict.Remove(id); // Удалить из списка "к удалению"
                    freshDict.Remove(id);    // Удалить из списка "к добавлению"
                }
                else
                {
                    // Процесс завершён — удаляем из коллекции
                    Processes.Remove(existingDict[id]);
                }
            }
        
            foreach (var newProc in freshDict.Values)
                Processes.Add(newProc);
        
            if (_selectedProcessIdBeforeRefresh.HasValue)
            {
                var procToSelect = Processes.FirstOrDefault(p => p.Id == _selectedProcessIdBeforeRefresh.Value);
                if (procToSelect != null && SelectedProcess?.Id != procToSelect.Id)
                {
                    // Важно: меняем SelectedProcess только если это другой объект
                    SelectedProcess = procToSelect;
                }
            }
            
            BuildHierarchy();
            UpdateCharts();
        });
    }
    
    public void RefreshProcesses()
    {
        _ = RefreshProcessesAsync();
    }
    
    public void ApplyFilters()
    {
        CollectionViewSource.GetDefaultView(Processes).Refresh();
    }
    
    public bool ChangePriority(ProcessPriorityClass newPriority)
    {
        if (SelectedProcess == null) return false;
        
        if (newPriority == ProcessPriorityClass.RealTime)
        {
            var result = System.Windows.MessageBox.Show(
                "Установка приоритета Realtime может сделать систему нестабильной!\nПродолжить?",
                "Предупреждение", 
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return false;
        }
        
        return ProcessService.SetProcessPriority(SelectedProcess.Id, newPriority);
    }
    
    public void LoadCpuAffinity()
    {
        if (SelectedProcess == null) return;
        try
        {
            using var proc = Process.GetProcessById(SelectedProcess.Id);
            var mask = proc.ProcessorAffinity;
            for (int i = 0; i < Cores.Count; i++)
                Cores[i].IsEnabled = AffinityHelper.IsCoreEnabled(mask, i);
        }
        catch { /* Нет доступа */ }
    }

    public bool ApplyCpuAffinity()
    {
        if (SelectedProcess == null) return false;
        try
        {
            bool[] boolMask = Cores.Select(c => c.IsEnabled).ToArray();
            using var proc = Process.GetProcessById(SelectedProcess.Id);
            var newMask = AffinityHelper.SetCoreMask(boolMask);
            proc.ProcessorAffinity = newMask;
            SelectedProcess.ProcessorAffinity = newMask;
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка применения маски: {ex.Message}");
            return false;
        }
    }
    
    private void LoadThreadsForSelected()
    {
        if (SelectedProcess == null) return;
        
        try
        {
            using var proc = Process.GetProcessById(SelectedProcess.Id);
            var threads = new ObservableCollection<ThreadInfo>();
            foreach (ProcessThread t in proc.Threads)
            {
                threads.Add(new ThreadInfo
                {
                    Id = t.Id,
                    Priority = t.PriorityLevel,
                    State = t.ThreadState,
                    CpuTime = t.TotalProcessorTime
                });
            }
            SelectedProcess.Threads = threads;
        }
        catch { /* Нет доступа к потокам */ }
    }
    
    public bool KillSelectedProcess()
    {
        if (SelectedProcess == null) return false;
        
        var result = System.Windows.MessageBox.Show(
            $"Завершить процесс \"{SelectedProcess.Name}\" (PID: {SelectedProcess.Id})?",
            "Подтверждение", 
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
            
        if (result != System.Windows.MessageBoxResult.Yes) return false;
        
        try
        {
            using var proc = Process.GetProcessById(SelectedProcess.Id);
            proc.Kill();
            RefreshProcesses();
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Не удалось завершить: {ex.Message}");
            return false;
        }
    }
    
    private void BuildHierarchy()
    {
        var nodes = Processes.ToDictionary(p => p.Id, p => new ProcessNode { Process = p });
        var rootNodes = new ObservableCollection<ProcessNode>();

        foreach (var node in nodes.Values)
        {
            if (node.Process.ParentId != 0 && nodes.ContainsKey(node.Process.ParentId))
            {
                nodes[node.Process.ParentId].Children.Add(node);
                nodes[node.Process.ParentId].NotifyChildrenCount();
            }
            else
            {
                rootNodes.Add(node);
            }
        }
        Hierarchy = rootNodes;
    }

    private void UpdateCharts()
    {
        // Топ 10 процессов по памяти
        var topMemory = Processes.OrderByDescending(p => p.MemoryUsage).Take(10).ToList();
    
        MemoryChartSeries.Clear();
        foreach (var p in topMemory)
        {
            MemoryChartSeries.Add(new PieSeries
            {
                Title = p.Name,
                Values = new ChartValues<double> { p.MemoryUsage / 1024.0 / 1024.0 },
                DataLabels = true
            });
        }
    }
    
    // INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}