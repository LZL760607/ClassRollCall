using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ClassRollCall.Views.Pages;

namespace ClassRollCall.Views;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private System.Windows.Controls.Button? _currentSelectedBtn;

    public MainWindow(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        NavHome(sender, e);
    }

    // 标题栏拖动
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    // 关闭改为隐藏，程序后台常驻托盘
    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    // 页面向上切入+淡入动画
    private void NavigateWithSlideUp(Page page)
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100));
        var slideOut = new ThicknessAnimation(
            new Thickness(0, 0, 0, 0),
            new Thickness(0, -15, 0, 0),
            TimeSpan.FromMilliseconds(100));
        slideOut.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };

        fadeOut.Completed += (s, args) =>
        {
            MainFrame.Navigate(page);
            MainFrame.Opacity = 0;
            MainFrame.Margin = new Thickness(0, 20, 0, 0);

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240));
            var slideIn = new ThicknessAnimation(
                new Thickness(0, 20, 0, 0),
                new Thickness(0, 0, 0, 0),
                TimeSpan.FromMilliseconds(240));
            slideIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };

            MainFrame.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            MainFrame.BeginAnimation(MarginProperty, slideIn);
        };

        MainFrame.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        MainFrame.BeginAnimation(MarginProperty, slideOut);
    }

    private void SetSelectedButton(System.Windows.Controls.Button btn)
    {
        if (_currentSelectedBtn != null)
            _currentSelectedBtn.ClearValue(System.Windows.Controls.Button.BackgroundProperty);
        btn.Background = new SolidColorBrush(Color.FromArgb(51, 255, 255, 255));
        btn.Foreground = Brushes.White;
        _currentSelectedBtn = btn;
    }

    private void NavHome(object? sender, RoutedEventArgs? e)
    {
        var page = _serviceProvider.GetRequiredService<HomePage>();
        NavigateWithSlideUp(page);
        SetSelectedButton(BtnHome);
    }

    private void NavStudent(object? sender, RoutedEventArgs? e)
    {
        var page = _serviceProvider.GetRequiredService<StudentManagePage>();
        NavigateWithSlideUp(page);
        SetSelectedButton(BtnStudent);
    }

    public void NavSetting(object? sender, RoutedEventArgs? e)
    {
        var page = _serviceProvider.GetRequiredService<SettingsPage>();
        NavigateWithSlideUp(page);
        SetSelectedButton(BtnSetting);
    }
}