using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ClassRollCall.Models;

namespace ClassRollCall.Services;

public class StudentService
{
    private readonly IConfigurationService _config;
    private readonly Random _random = new();

    public ObservableCollection<StudentInfo> Students { get; } = new();
    public int RollCount { get; set; } = 1;
    public bool AutoWeightEnabled { get; set; } = true;
    public double DecayFactor { get; set; } = 0.7;

    public event Action? WeightsChanged;

    public StudentService(IConfigurationService config)
    {
        _config = config;
    }

    public void LoadFromStorage()
    {
        var loaded = _config.GetConfiguration<List<StudentInfo>>("Students");
        if (loaded != null)
        {
            Students.Clear();
            foreach (var s in loaded) Students.Add(s);
        }
        else
        {
            AddStudent("张三");
            AddStudent("李四");
            AddStudent("王五");
            AddStudent("赵六");
            AddStudent("孙七");
        }
        RollCount = _config.GetConfiguration<int?>("RollCount") ?? 1;
        AutoWeightEnabled = _config.GetConfiguration<bool?>("AutoWeightEnabled") ?? true;
        DecayFactor = _config.GetConfiguration<double?>("DecayFactor") ?? 0.7;
    }

    public void SaveToStorage()
    {
        _config.SetConfiguration("Students", Students.ToList());
        _config.SetConfiguration("RollCount", RollCount);
        _config.SetConfiguration("AutoWeightEnabled", AutoWeightEnabled);
        _config.SetConfiguration("DecayFactor", DecayFactor);
        _config.Save();
    }

    private void NotifyWeightsChanged() => WeightsChanged?.Invoke();

    public void AddStudent(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Students.Any(s => s.Name == name)) return;
        Students.Add(new StudentInfo { Name = name, Weight = 1.0 });
        NormalizeWeights();
        SaveToStorage();
    }

    public void RemoveStudent(string name)
    {
        var target = Students.FirstOrDefault(s => s.Name == name);
        if (target != null) { Students.Remove(target); NormalizeWeights(); SaveToStorage(); }
    }

    public StudentInfo RollRandomStudent()
    {
        if (Students.Count == 0) return new StudentInfo { Name = "暂无学生" };
        double total = Students.Sum(s => s.Weight);
        double value = _random.NextDouble() * total;
        double current = 0;
        foreach (var s in Students)
        {
            current += s.Weight;
            if (value <= current) { UpdateWeightAfterCalled(s); return s; }
        }
        return Students.Last();
    }

    public List<StudentInfo> RollRandomStudents(int count)
    {
        var result = new List<StudentInfo>();
        if (Students.Count == 0 || count <= 0) return result;
        count = Math.Min(count, Students.Count);
        var pool = new List<StudentInfo>(Students);
        for (int i = 0; i < count; i++)
        {
            double total = pool.Sum(s => s.Weight);
            double value = _random.NextDouble() * total;
            double current = 0;
            StudentInfo? selected = null;
            foreach (var s in pool)
            {
                current += s.Weight;
                if (value <= current) { selected = s; break; }
            }
            selected ??= pool.Last();
            result.Add(selected);
            pool.Remove(selected);
            UpdateWeightAfterCalled(selected);
        }
        return result;
    }

    private void UpdateWeightAfterCalled(StudentInfo called)
    {
        if (!AutoWeightEnabled) return;
        called.LastCalledTime = DateTime.Now;
        if (called.IsWeightLocked) return;
        double old = called.Weight;
        double nw = Math.Max(0.1, old * DecayFactor);
        double diff = old - nw;
        called.Weight = Math.Round(nw, 2);
        var others = Students.Where(s => s != called && !s.IsWeightLocked).ToList();
        double ot = others.Sum(s => s.Weight);
        if (ot > 0)
            foreach (var s in others)
                s.Weight = Math.Round(s.Weight + diff * (s.Weight / ot), 2);
        NotifyWeightsChanged();
        SaveToStorage();
    }

    public void SetWeight(string name, double weight)
    {
        var s = Students.FirstOrDefault(x => x.Name == name);
        if (s == null || s.IsWeightLocked) return;
        s.Weight = Math.Round(Math.Clamp(weight, 0.1, Students.Count), 2);
        NormalizeWeights();
        NotifyWeightsChanged();
        SaveToStorage();
    }

    public void ToggleLock(string name)
    {
        var s = Students.FirstOrDefault(x => x.Name == name);
        if (s == null) return;
        s.IsWeightLocked = !s.IsWeightLocked;
        NormalizeWeights();
        NotifyWeightsChanged();
        SaveToStorage();
    }

    public void NormalizeWeights()
    {
        int n = Students.Count;
        if (n == 0) return;
        var locked = Students.Where(s => s.IsWeightLocked).ToList();
        var unlocked = Students.Where(s => !s.IsWeightLocked).ToList();
        double ls = locked.Sum(s => s.Weight);
        double target = n - ls;
        if (unlocked.Count > 0)
        {
            double cur = unlocked.Sum(s => s.Weight);
            double scale = cur > 0 ? target / cur : 1.0;
            foreach (var s in unlocked)
                s.Weight = Math.Round(Math.Clamp(s.Weight * scale, 0.1, n), 2);
        }
        NotifyWeightsChanged();
    }

    public void ResetAllWeights()
    {
        foreach (var s in Students)
            if (!s.IsWeightLocked) s.Weight = 1.0;
        NotifyWeightsChanged();
        SaveToStorage();
    }

    public List<(string Name, double Probability)> GetProbabilities()
    {
        double total = Students.Sum(s => s.Weight);
        if (total <= 0) return Students.Select(s => (s.Name, 0.0)).ToList();
        return Students.Select(s => (s.Name, Math.Round(s.Weight / total * 100, 1))).ToList();
    }
}
