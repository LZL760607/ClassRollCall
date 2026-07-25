using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace ClassRollCall.Views;

public partial class SplashScreen : Window
{
    private readonly IServiceProvider _serviceProvider;

    public SplashScreen(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();

        var chrome = new WindowChrome
        {
            CaptionHeight = 0,
            ResizeBorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0)
        };
        WindowChrome.SetWindowChrome(this, chrome);

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ((Storyboard)FindResource("FadeInStoryboard")).Begin();
        ((Storyboard)FindResource("SpinnerStoryboard")).Begin();  // ← 启动旋转
        ((Storyboard)FindResource("ProgressStoryboard")).Begin();
    }

    private void OnProgressCompleted(object? sender, EventArgs e)
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        Close();
    }
}
