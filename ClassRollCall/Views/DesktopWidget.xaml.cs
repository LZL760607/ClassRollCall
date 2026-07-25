using ClassRollCall.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ClassRollCall.Views;

public partial class DesktopWidget : Window
{
    private readonly StudentService _studentService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DispatcherTimer _longPressTimer;
    private readonly DispatcherTimer _rollbackTimer;
    private const int LongPressMs = 1500;
    private const double PathTotalLength = 216;
    private const double DragDetectThreshold = 12;

    private DateTime _pressStartTime;
    private Point _pressStartPos;
    private bool _isDragging;
    private bool _isLongPressTriggered;
    private bool _isPanelOpen;
    private double _currentProgress;

    // Storyboard 缓存
    private Storyboard? _panelOpenSb;
    private Storyboard? _panelCloseSb;
    private EventHandler? _closeCompletedHandler;

    // 手动关闭标记：防止 MouseLeave 死循环
    private bool _manualClose;

    public DesktopWidget(StudentService studentService, IServiceProvider serviceProvider)
    {
        _studentService = studentService;
        _serviceProvider = serviceProvider;
        InitializeComponent();

        _longPressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _longPressTimer.Tick += LongPressTimer_Tick;

        _rollbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _rollbackTimer.Tick += RollbackTimer_Tick;

        Loaded += OnLoaded;
        MouseLeftButtonDown += OnMouseDown;
        MouseLeftButtonUp += OnMouseUp;
        MouseMove += OnMouseMove;

        // 悬浮窗鼠标离开后半透明（不是隐藏，防止 Hide/Show 循环）
        MouseEnter += (s, e) => Opacity = 1.0;
        MouseLeave += (s, e) =>
        {
            if (!_isPanelOpen && !_manualClose)
                Opacity = 0.6;
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Left = SystemParameters.WorkArea.Width - Width - 40;
        Top = SystemParameters.WorkArea.Height - Height - 40;
        CustomCountBox.Text = _studentService.RollCount.ToString();

        // 在 Loaded 中缓存 Storyboard（确保模板已加载）
        _panelOpenSb = (Storyboard)FindResource("PanelOpenStoryboard");
        _panelCloseSb = (Storyboard)FindResource("PanelCloseStoryboard");
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _rollbackTimer.Stop();
        _pressStartTime = DateTime.Now;
        _pressStartPos = e.GetPosition(this);
        _isDragging = false;
        _isLongPressTriggered = false;
        _currentProgress = 0;

        if (_isPanelOpen) CloseCountPanel();

        ProgressPath.Visibility = Visibility.Visible;
        ProgressPath.StrokeDashArray = new DoubleCollection([0, PathTotalLength]);
        _longPressTimer.Start();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _isDragging) return;

        Point currentPos = e.GetPosition(this);
        double distance = Math.Sqrt(
            Math.Pow(currentPos.X - _pressStartPos.X, 2) +
            Math.Pow(currentPos.Y - _pressStartPos.Y, 2));

        if (distance > DragDetectThreshold)
        {
            _isDragging = true;
            _longPressTimer.Stop();
            ProgressPath.Visibility = Visibility.Collapsed;
            DragMove();
        }
    }

    private void LongPressTimer_Tick(object? sender, EventArgs e)
    {
        double elapsed = (DateTime.Now - _pressStartTime).TotalMilliseconds;
        double progress = Math.Min(elapsed / LongPressMs, 1.0);
        _currentProgress = progress * PathTotalLength;
        ProgressPath.StrokeDashArray = new DoubleCollection([_currentProgress, PathTotalLength]);

        if (progress >= 1.0)
        {
            _longPressTimer.Stop();
            _isLongPressTriggered = true;
            ProgressPath.Visibility = Visibility.Collapsed;
            TriggerRollCall();
        }
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _longPressTimer.Stop();

        if (_isDragging || _isLongPressTriggered)
        {
            ProgressPath.Visibility = Visibility.Collapsed;
            return;
        }

        double elapsed = (DateTime.Now - _pressStartTime).TotalMilliseconds;
        if (elapsed < LongPressMs)
            _rollbackTimer.Start();
    }

    private void RollbackTimer_Tick(object? sender, EventArgs e)
    {
        _currentProgress -= PathTotalLength * 0.08;
        if (_currentProgress <= 0)
        {
            _rollbackTimer.Stop();
            ProgressPath.Visibility = Visibility.Collapsed;
            ToggleCountPanel();
            return;
        }
        ProgressPath.StrokeDashArray = new DoubleCollection([_currentProgress, PathTotalLength]);
    }

    private void ToggleCountPanel()
    {
        if (_isPanelOpen) CloseCountPanel();
        else OpenCountPanel();
    }

    private void OpenCountPanel()
    {
        _isPanelOpen = true;
        CountPanel.Visibility = Visibility.Visible;
        _panelOpenSb?.Stop();
        _panelOpenSb?.Begin();
    }

    private void CloseCountPanel()
    {
        _isPanelOpen = false;

        if (_panelCloseSb == null) return;

        _panelCloseSb.Stop();

        // 移除旧 handler 防止泄漏
        if (_closeCompletedHandler != null)
            _panelCloseSb.Completed -= _closeCompletedHandler;

        _closeCompletedHandler = (s, e) =>
        {
            CountPanel.Visibility = Visibility.Collapsed;
            PanelTranslate.Y = -20;
            if (_panelCloseSb != null)
                _panelCloseSb.Completed -= _closeCompletedHandler;
            _closeCompletedHandler = null;
        };

        _panelCloseSb.Completed += _closeCompletedHandler;
        _panelCloseSb.Begin();
    }

    private void CountSelect_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int count))
        {
            _studentService.RollCount = count;
            CustomCountBox.Text = count.ToString();
        }
    }

    private void CustomCountBox_GotFocus(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("osk.exe", "/numpad") { UseShellExecute = true });
        }
        catch { }
    }

    private void CustomCountBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (int.TryParse(CustomCountBox.Text.Trim(), out int count) && count >= 1)
            _studentService.RollCount = count;
    }

    private void OpenMainWindow_Click(object sender, RoutedEventArgs e)
    {
        CloseCountPanel();
        if (Application.Current is App app)
            app.ShowMainWindow();
    }

    private void CloseWidget_Click(object sender, RoutedEventArgs e)
    {
        CloseCountPanel();
        _manualClose = true;
        Hide();
    }

    /// <summary>外部调用 Show 时重置手动关闭标记</summary>
    public new void Show()
    {
        _manualClose = false;
        base.Show();
    }

    private void TriggerRollCall()
    {
        // 按压反馈
        var scaleAnim = new DoubleAnimation(1, 0.92, TimeSpan.FromMilliseconds(80))
        {
            AutoReverse = true,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        WidgetBody.BeginAnimation(UIElement.OpacityProperty, scaleAnim);

        int count = _studentService.RollCount;
        var results = _studentService.RollRandomStudents(count);
        var names = results.ConvertAll(s => s.Name);

        var popup = new ResultPopupWindow(names);
        popup.Show();
    }
}
