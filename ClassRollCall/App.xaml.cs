using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Windows;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;
using ClassRollCall.Services;
using ClassRollCall.Views;
using ClassRollCall.ViewModels;
using ClassRollCall.Views.Pages;

namespace ClassRollCall;

public partial class App : Application
{
    private readonly IHost _host;
    private Forms.NotifyIcon? _trayIcon;

    public App()
    {
        _host = CreateHostBuilder().Build();
    }

    private static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IConfigurationService, ConfigurationService>();
                services.AddSingleton<StudentService>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<DesktopWidget>();
                services.AddSingleton<WeightManageWindow>();
                services.AddSingleton<MainViewModel>();
                services.AddTransient<Views.SplashScreen>();
                services.AddTransient<HomePage>();
                services.AddTransient<StudentManagePage>();
                services.AddTransient<SettingsPage>();
            });

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var studentService = _host.Services.GetRequiredService<StudentService>();
        studentService.LoadFromStorage();

        InitTrayIcon();

        var splash = _host.Services.GetRequiredService<Views.SplashScreen>();
        splash.Show();

        splash.Closed += (s, args) =>
        {
            var widget = _host.Services.GetRequiredService<DesktopWidget>();
            widget.Show();
        };

        base.OnStartup(e);
    }

    // ==================== 托盘 ====================

    private void InitTrayIcon()
    {
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = Drawing.SystemIcons.Application,
            Text = "ClassRollCall",
            Visible = true
        };

        _trayIcon.MouseClick += (s, args) =>
        {
            if (args.Button == Forms.MouseButtons.Left)
                ShowTrayMenu();
        };

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("打开主界面", null, (s, e) => ShowMainWindow());
        menu.Items.Add("应用设置", null, (s, e) => OpenSettingsPage());
        menu.Items.Add("-");
        menu.Items.Add("重启程序", null, (s, e) => RestartApp());
        menu.Items.Add("退出程序", null, (s, e) => ExitApp());
        _trayIcon.ContextMenuStrip = menu;
    }

    private void ShowTrayMenu()
    {
        typeof(Forms.NotifyIcon).InvokeMember("ShowContextMenu",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.InvokeMethod,
            null, _trayIcon, null);
    }

    // ==================== 窗口操作 ====================

    public void ShowMainWindow()
    {
        var main = _host.Services.GetRequiredService<MainWindow>();
        main.Show();
        main.Activate();
        main.WindowState = WindowState.Normal;
    }

    private void OpenSettingsPage()
    {
        ShowMainWindow();
        var main = _host.Services.GetRequiredService<MainWindow>();
        main.NavSetting(null, null);
    }

    private void RestartApp()
    {
        Process.Start(Process.GetCurrentProcess().MainModule?.FileName ?? "ClassRollCall.exe");
        ExitApp();
    }

    private void ExitApp()
    {
        var studentService = _host.Services.GetRequiredService<StudentService>();
        studentService.SaveToStorage();

        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }

        _host.StopAsync().Wait();
        Current.Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
