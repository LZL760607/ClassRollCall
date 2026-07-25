using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
    public bool EnhancedFairMode { get; set; }

    public event Action? WeightsChanged;

    public StudentService(IConfigurationService config)
    {
        _config = config;
    }

    // ==================== 持久化 ====================

    public void LoadFromStorage()
    {
        var loaded = _config.GetConfiguration<List<StudentInfo>>("Students");
        if (loaded != null)
        {
            Students.Clear();
            foreach (var s in loaded)
                Students.Add(s);
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

    // ==================== 学生增删 ====================

    public void AddStudent(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Students.Any(s => s.Name == name))
            return;
        Students.Add(new StudentInfo { Name = name, Weight = 1.0 });
        NormalizeWeights();
        SaveToStorage();
    }

    public void RemoveStudent(string name)
    {
        var target = Students.FirstOrDefault(s => s.Name == name);
        if (target != null)
        {
            Students.Remove(target);
            NormalizeWeights();
            SaveToStorage();
        }
    }

    // ==================== 普通点名（加权随机） ====================

    public StudentInfo RollRandomStudent()
    {
        if (Students.Count == 0)
            return new StudentInfo { Name = "暂无学生" };

        if (EnhancedFairMode)
            return EnhancedFairRoll(1)[0];

        double totalWeight = Students.Sum(s => s.Weight);
        double value = _random.NextDouble() * totalWeight;

        double current = 0;
        foreach (var student in Students)
        {
            current += student.Weight;
            if (value <= current)
            {
                UpdateWeightAfterCalled(student);
                return student;
            }
        }
        return Students.Last();
    }

    public List<StudentInfo> RollRandomStudents(int count)
    {
        if (Students.Count == 0 || count <= 0) return new List<StudentInfo>();
        count = Math.Min(count, Students.Count);

        if (EnhancedFairMode)
            return EnhancedFairRoll(count);

        var result = new List<StudentInfo>();
        var tempPool = new List<StudentInfo>(Students);

        for (int i = 0; i < count; i++)
        {
            double totalWeight = tempPool.Sum(s => s.Weight);
            double value = _random.NextDouble() * totalWeight;

            double current = 0;
            StudentInfo? selected = null;
            foreach (var student in tempPool)
            {
                current += student.Weight;
                if (value <= current)
                {
                    selected = student;
                    break;
                }
            }
            selected ??= tempPool.Last();

            result.Add(selected);
            tempPool.Remove(selected);
            UpdateWeightAfterCalled(selected);
        }
        return result;
    }

    // ==================== 高度公平模式 ====================

    /// <summary>
    /// 高度公平模式：多层 Fisher-Yates 迭代 + 投票机制。
    /// 强制所有人权重=1，忽略锁定状态。
    /// 迭代3轮，每轮用独立 SHA256 种子洗牌，统计投票选出得票最高的 N 人。
    /// 时间复杂度 O(3×N)，学生数 &lt;200 时几乎无感知。
    /// </summary>
    public List<StudentInfo> EnhancedFairRoll(int count)
    {
        const int iterations = 3;
        var allNames = Students.Select(s => s.Name).ToList();
        var voteCount = new Dictionary<string, int>();

        for (int round = 0; round < iterations; round++)
        {
            // 独立种子：SHA256(Guid + Ticks + round)
            string raw = $"{Guid.NewGuid():N}-{DateTime.UtcNow.Ticks}-{round}";
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            int seed = BitConverter.ToInt32(hash, 0);

            var rng = new Random(seed);
            var pool = new List<string>(allNames);

            // Fisher-Yates 洗牌
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            // 取前 count 个投票
            for (int i = 0; i < count; i++)
            {
                string name = pool[i];
                voteCount.TryGetValue(name, out int v);
                voteCount[name] = v + 1;
            }
        }

        // 按得票降序，取前 count
        var ranked = voteCount
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .Take(count)
            .Select(kv => kv.Key)
            .ToList();

        // 如有平票且超出 count，用最终轮洗牌打破平局
        if (ranked.Count < count)
        {
            var remaining = allNames.Except(ranked).ToList();
            string raw = $"{Guid.NewGuid():N}-tiebreak";
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            var rng = new Random(BitConverter.ToInt32(hash, 0));
            for (int i = remaining.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (remaining[i], remaining[j]) = (remaining[j], remaining[i]);
            }
            ranked.AddRange(remaining.Take(count - ranked.Count));
        }

        return ranked
            .Select(name => Students.First(s => s.Name == name))
            .ToList();
    }

    // ==================== 权重算法 ====================

    private void UpdateWeightAfterCalled(StudentInfo called)
    {
        if (!AutoWeightEnabled || EnhancedFairMode) return;

        called.LastCalledTime = DateTime.Now;
        if (called.IsWeightLocked) return;

        double oldWeight = called.Weight;
        double newWeight = Math.Max(0.1, oldWeight * DecayFactor);
        double diff = oldWeight - newWeight;
        called.Weight = Math.Round(newWeight, 2);

        var others = Students.Where(s => s != called && !s.IsWeightLocked).ToList();
        double othersTotal = others.Sum(s => s.Weight);
        if (othersTotal > 0)
        {
            foreach (var s in others)
                s.Weight = Math.Round(s.Weight + diff * (s.Weight / othersTotal), 2);
        }

        NotifyWeightsChanged();
        SaveToStorage();
    }

    public void SetWeight(string name, double weight)
    {
        var student = Students.FirstOrDefault(s => s.Name == name);
        if (student == null || student.IsWeightLocked) return;
        double maxWeight = Students.Count;
        student.Weight = Math.Round(Math.Clamp(weight, 0.1, maxWeight), 2);
        NormalizeWeights();
        NotifyWeightsChanged();
        SaveToStorage();
    }

    public void ToggleLock(string name)
    {
        var student = Students.FirstOrDefault(s => s.Name == name);
        if (student == null) return;
        student.IsWeightLocked = !student.IsWeightLocked;
        NormalizeWeights();
        NotifyWeightsChanged();
        SaveToStorage();
    }

    public void NormalizeWeights()
    {
        int total = Students.Count;
        if (total == 0) return;

        var locked = Students.Where(s => s.IsWeightLocked).ToList();
        var unlocked = Students.Where(s => !s.IsWeightLocked).ToList();
        double lockedSum = locked.Sum(s => s.Weight);
        double targetUnlockedSum = total - lockedSum;

        if (unlocked.Count > 0)
        {
            double currentUnlockedSum = unlocked.Sum(s => s.Weight);
            double scale = currentUnlockedSum > 0 ? targetUnlockedSum / currentUnlockedSum : 1.0;
            foreach (var s in unlocked)
                s.Weight = Math.Round(Math.Clamp(s.Weight * scale, 0.1, total), 2);
        }
        NotifyWeightsChanged();
    }

    /// <summary>重置未锁定的人为 1.0</summary>
    public void ResetAllWeights()
    {
        foreach (var s in Students)
            if (!s.IsWeightLocked) s.Weight = 1.0;
        NotifyWeightsChanged();
        SaveToStorage();
    }

    /// <summary>强制重置所有人（不管锁定）为 1.0</summary>
    public void ResetAllWeightsForce()
    {
        foreach (var s in Students) s.Weight = 1.0;
        NotifyWeightsChanged();
        SaveToStorage();
    }

    public List<(string Name, double Probability)> GetProbabilities()
    {
        double total = Students.Sum(s => s.Weight);
        if (total <= 0)
            return Students.Select(s => (s.Name, 0.0)).ToList();
        return Students.Select(s => (s.Name, Math.Round(s.Weight / total * 100, 1))).ToList();
    }
}
