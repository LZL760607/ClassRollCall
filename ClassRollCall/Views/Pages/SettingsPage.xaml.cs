using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using ClassRollCall.Services;
using ClassRollCall.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ClassRollCall.Views.Pages;

public partial class SettingsPage : Page
{
    private readonly StudentService _studentService;
    private readonly IServiceProvider _serviceProvider;
    private readonly System.Windows.Threading.DispatcherTimer _seedTimer;

    private bool _isUpdating;
    private bool _isLoaded;
    private DesktopWidget? _widget;

    public SettingsPage(StudentService studentService, IServiceProvider serviceProvider)
    {
        _studentService = studentService;
        _serviceProvider = serviceProvider;
        InitializeComponent();
        Loaded += OnPageLoaded;

        _seedTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(15)
        };
        _seedTimer.Tick += (s, e) => RefreshSeed();

        _studentService.WeightsChanged += RefreshProbabilities;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        _isUpdating = true;
        CustomCountInput.Text = _studentService.RollCount.ToString();
        _isUpdating = false;

        _widget = _serviceProvider.GetService<DesktopWidget>();

        Dispatcher.BeginInvoke(new Action(() =>
        {
            WidgetSwitch.IsChecked = _widget?.IsVisible == true;
            AutoWeightSwitch.IsChecked = _studentService.AutoWeightEnabled;
            DecaySlider.Value = _studentService.DecayFactor;
            DecayLabel.Text = $"{_studentService.DecayFactor:F2}";

            RefreshSeed();
            _seedTimer.Start();
            UpdateFormula();
            RefreshProbabilities();
            UpdateFormulaVisibility();

            _isLoaded = true;
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    // ==================== 点名人数 ====================

    private void CountBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int count))
        {
            _studentService.RollCount = count;
            _isUpdating = true;
            CustomCountInput.Text = count.ToString();
            _isUpdating = false;
        }
    }

    private void CustomCountInput_GotFocus(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("osk.exe", "/numpad") { UseShellExecute = true });
        }
        catch { }
    }

    private void CustomCountInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;
        if (int.TryParse(CustomCountInput.Text.Trim(), out int count) && count >= 1)
            _studentService.RollCount = count;
    }

    // ==================== 自动权重 ====================

    private void AutoWeight_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;

        bool enabled = AutoWeightSwitch.IsChecked == true;
        _studentService.AutoWeightEnabled = enabled;
        _studentService.SaveToStorage();
        UpdateFormulaVisibility();
        UpdateFormula();

        if (!enabled)
            _studentService.ResetAllWeights();
    }

    private void UpdateFormulaVisibility()
    {
        FormulaSection.Visibility = _studentService.AutoWeightEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    // ==================== 衰减系数 ====================

    private void Decay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isLoaded) return;

        double val = Math.Round(DecaySlider.Value, 2);
        _studentService.DecayFactor = val;
        DecayLabel.Text = $"{val:F2}";
        _studentService.SaveToStorage();
        UpdateFormula();
    }

    // ==================== 随机种子 ====================

    private void RefreshSeed()
    {
        string entropy = Guid.NewGuid().ToString("N") + DateTime.UtcNow.Ticks;
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(entropy));
        string seed = ToBase62(hash).Substring(0, 15);
        SeedText.Text = seed;
    }

    private static string ToBase62(byte[] bytes)
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var sb = new StringBuilder();
        var num = System.Numerics.BigInteger.Abs(
            new System.Numerics.BigInteger(bytes.Concat(new byte[] { 0 }).ToArray()));
        while (num > 0 && sb.Length < 30)
        {
            sb.Insert(0, chars[(int)(num % 62)]);
            num /= 62;
        }
        while (sb.Length < 15)
            sb.Append(chars[Random.Shared.Next(62)]);
        return sb.ToString();
    }

    // ==================== 公式展示 ====================

    private void UpdateFormula()
    {
        DecayFormula.Text = $"decay={_studentService.DecayFactor:F2}";
        FormulaDetail.Text = _studentService.AutoWeightEnabled
            ? $"被点名后：权重 × {_studentService.DecayFactor:F2}，" +
              $"差值按比例分配给其他未锁定的人。总权重恒等于 {_studentService.Students.Count}。"
            : "自动权重调节已关闭。";
    }

    // ==================== 概率分布 ====================

    private void RefreshProbabilities()
    {
        var probs = _studentService.GetProbabilities();
        var parts = probs.Select(p =>
        {
            var student = _studentService.Students.First(s => s.Name == p.Name);
            return $"{p.Name} {student.Weight:F1}({p.Probability:F1}%)";
        });
        ProbabilityText.Text = string.Join("    ", parts);
    }

    // ==================== 悬浮窗 ====================

    private void WidgetSwitch_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        _widget ??= _serviceProvider.GetService<DesktopWidget>();
        _widget?.Show();
    }

    private void WidgetSwitch_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        _widget?.Hide();
    }
}
