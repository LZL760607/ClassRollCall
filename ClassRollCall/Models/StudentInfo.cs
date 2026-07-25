using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClassRollCall.Models;

public class StudentInfo : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private double _weight = 1.0;
    private bool _isWeightLocked;
    private DateTime _lastCalledTime = DateTime.Now;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public double Weight
    {
        get => _weight;
        set { _weight = value; OnPropertyChanged(); }
    }

    public bool IsWeightLocked
    {
        get => _isWeightLocked;
        set { _isWeightLocked = value; OnPropertyChanged(); }
    }

    public DateTime LastCalledTime
    {
        get => _lastCalledTime;
        set { _lastCalledTime = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
