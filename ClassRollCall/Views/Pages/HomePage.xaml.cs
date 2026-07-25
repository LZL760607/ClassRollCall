using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ClassRollCall.Services;

namespace ClassRollCall.Views.Pages;

public partial class HomePage : Page
{
    private readonly StudentService _studentService;
    private readonly DispatcherTimer _rollTimer;
    private int _currentSpeed = 60;
    private const int TotalDuration = 2200;
    private DateTime _startTime;
    private bool _isAnimating;

    public HomePage(StudentService studentService)
    {
        _studentService = studentService;
        InitializeComponent();

        _rollTimer = new DispatcherTimer();
        _rollTimer.Tick += RollTimer_Tick;

        Loaded += (s, e) =>
        {
            FairModeSwitch.IsChecked = _studentService.EnhancedFairMode;
            FairFormulaPanel.Visibility = _studentService.EnhancedFairMode
                ? Visibility.Visible : Visibility.Collapsed;
        };
    }

    // ==================== 高度公平模式 ====================

    private void FairMode_Checked(object sender, RoutedEventArgs e)
    {
        var dialog = new StyledDialog(
            "高度公平模式",
            "使用 3 轮独立种子 Fisher-Yates 洗牌 + 多数投票，消除单次伪随机的偏差。\n\n" +
            "开启后强制权重 = 1，无视锁定，结果去重。\n" +
            "学生 < 200 人时几乎无性能影响。\n\n" +
            "适合分发奖品、抽取劳动等场景。",
            "开 启",
            "取 消");

        dialog.ShowDialog();

        if (dialog.Confirmed)
        {
            _studentService.EnhancedFairMode = true;
            FairFormulaPanel.Visibility = Visibility.Visible;
        }
        else
        {
            FairModeSwitch.IsChecked = false;
        }
    }



    private void FairMode_Unchecked(object sender, RoutedEventArgs e)
    {
        _studentService.EnhancedFairMode = false;
        FairFormulaPanel.Visibility = Visibility.Collapsed;
    }

    // ==================== 滚动点名 ====================

    private void RollTimer_Tick(object? sender, EventArgs e)
    {
        RollText.Text = _studentService.RollRandomStudent().Name;

        double elapsed = (DateTime.Now - _startTime).TotalMilliseconds;
        double progress = Math.Min(elapsed / TotalDuration, 1.0);
        _currentSpeed = (int)(60 + 300 * Math.Pow(progress, 3));
        _rollTimer.Interval = TimeSpan.FromMilliseconds(_currentSpeed);

        if (progress >= 1.0)
        {
            _rollTimer.Stop();
            ShowResultAnimation(RollText.Text);
            StartBtn.IsEnabled = true;
            StartBtn.Content = "重新点名";
        }
    }

    private void StartRoll_Click(object sender, RoutedEventArgs e)
    {
        if (_studentService.Students.Count < 2)
        {
            MessageBox.Show("请先在学生管理中添加至少2名学生");
            return;
        }

        if (_isAnimating)
            HideResultPanel();

        StartBtn.IsEnabled = false;
        _startTime = DateTime.Now;
        _currentSpeed = 60;
        _rollTimer.Interval = TimeSpan.FromMilliseconds(_currentSpeed);
        _rollTimer.Start();
    }

    private void ShowResultAnimation(string name)
    {
        _isAnimating = true;
        ResultText.Text = name;
        ResultMask.Visibility = Visibility.Visible;

        ResultPanel.Width = 0;
        ResultPanel.Height = 4;
        ResultPanel.Child.Opacity = 0;

        var widthAnim = new DoubleAnimation(0, 420, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        widthAnim.Completed += (s, args) =>
        {
            var heightAnim = new DoubleAnimation(4, 180, TimeSpan.FromMilliseconds(320))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var contentFade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
            {
                BeginTime = TimeSpan.FromMilliseconds(120)
            };
            ResultPanel.BeginAnimation(FrameworkElement.HeightProperty, heightAnim);
            ResultPanel.Child.BeginAnimation(OpacityProperty, contentFade);
        };
        ResultPanel.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);
    }

    private void ResultMask_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        HideResultPanel();
    }

    private void HideResultPanel()
    {
        ResultPanel.BeginAnimation(FrameworkElement.WidthProperty, null);
        ResultPanel.BeginAnimation(FrameworkElement.HeightProperty, null);
        ResultPanel.Child.BeginAnimation(OpacityProperty, null);

        ResultMask.Visibility = Visibility.Collapsed;
        ResultPanel.Width = 0;
        ResultPanel.Height = 4;
        ResultPanel.Child.Opacity = 0;
        _isAnimating = false;
    }
}
