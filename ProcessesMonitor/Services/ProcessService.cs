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
        var parentIds = new Dictionary<int, int>();

        // ParentId для всех процессов одним запросом
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProcessId, ParentProcessId FROM Win32_Process");
            foreach (ManagementObject obj in searcher.Get())
            {
                parentIds[(int)(uint)obj["ProcessId"]] = (int)(uint)obj["ParentProcessId"];
            }
        }
        catch { }

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
                    ThreadCount = p.Threads.Count,
                    ParentId = parentIds.ContainsKey(p.Id) ? parentIds[p.Id] : 0
                });
            }
            catch { }
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