using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Windows;
using ProcessesMonitor.Models;

namespace ProcessesMonitor.Services;

public class ProcessService
{
    public static List<ProcessInfo> GetAllProcesses()
    {
        var processList = new List<ProcessInfo>();
        var processes = Process.GetProcesses();
    
        foreach (var p in processes)
        {
            try
            {
                processList.Add(new ProcessInfo
                {
                    Id = p.Id,
                    Name = p.ProcessName,
                    Priority = p.PriorityClass,
                    MemoryUsage = p.WorkingSet64,
                    ThreadCount = p.Threads.Count
                });
            }
            catch { /* Пропускаем процессы без доступа */ }
        }
        return processList;
    }
    
    public static bool SetProcessPriority(int processId, ProcessPriorityClass priority)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            process.PriorityClass = priority;
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}");
            return false;
        }
    }
    
    public static int GetParentProcessId(int processId)
    {
        using (var query = new ManagementObjectSearcher(
                   $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {processId}"))
        {
            var result = query.Get().Cast<ManagementObject>().FirstOrDefault();
            return (int)(uint)result["ParentProcessId"];
        }
    }
}