using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using ClassRollCall.Services;
using ClassRollCall.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ClassRollCall.Views.Pages;

public partial class StudentManagePage : Page
{
    private readonly StudentService _studentService;
    private readonly IServiceProvider _serviceProvider;
    private DesktopWidget? _widget;
    private WeightManageWindow? _weightWindow;

    private const string TriggerWord = "manager";
    private const string CorrectPassword = "100504";

    public StudentManagePage(StudentService studentService, IServiceProvider serviceProvider)
    {
        _studentService = studentService;
        _serviceProvider = serviceProvider;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        StudentList.ItemsSource = _studentService.Students;
        _widget = _serviceProvider.GetService<DesktopWidget>();
        _weightWindow = _serviceProvider.GetService<WeightManageWindow>();
    }

    // ==================== 隐藏入口 ====================

    private void NameInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (NameInput.Text.Trim().Equals(TriggerWord, StringComparison.OrdinalIgnoreCase))
        {
            NameInput.Text = string.Empty;
            ShowPasswordPanel();
        }
    }

    private void NameInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter &&
            NameInput.Text.Trim().Equals(TriggerWord, StringComparison.OrdinalIgnoreCase))
        {
            NameInput.Text = string.Empty;
            ShowPasswordPanel();
        }
    }

    private void ShowPasswordPanel()
    {
        PasswordPanel.Visibility = Visibility.Visible;
        PasswordBox.Password = string.Empty;
        PasswordBox.Focus();

        PasswordPanel.RenderTransform = PanelScale;

        var scaleAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        PanelScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        PasswordPanel.BeginAnimation(OpacityProperty, fadeIn);
    }

    private void HidePasswordPanel()
    {
        var scaleAnim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        scaleAnim.Completed += (s, a) => PasswordPanel.Visibility = Visibility.Collapsed;
        PanelScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(160));
        PasswordPanel.BeginAnimation(OpacityProperty, fadeOut);
    }

    private void ShakePasswordPanel()
    {
        var tg = new TransformGroup();
        tg.Children.Add(new ScaleTransform(1, 1));
        tg.Children.Add(new TranslateTransform());
        PasswordPanel.RenderTransform = tg;

        var tt = (TranslateTransform)tg.Children[1];
        var shake = new DoubleAnimation(0, 8, TimeSpan.FromMilliseconds(50))
        {
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(3),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        tt.BeginAnimation(TranslateTransform.XProperty, shake);

        PasswordPanel.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0x11, 0x23));
        var delay = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(600)
        };
        delay.Tick += (s, a) =>
        {
            delay.Stop();
            PasswordPanel.BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));
            PasswordBox.Password = string.Empty;
        };
        delay.Start();
    }

    private void ConfirmPassword_Click(object sender, RoutedEventArgs e)
    {
        if (PasswordBox.Password == CorrectPassword)
        {
            HidePasswordPanel();
            _weightWindow?.Show();
            _weightWindow?.Activate();
        }
        else
        {
            ShakePasswordPanel();
        }
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            ConfirmPassword_Click(sender, e);
    }

    private void CancelPassword_Click(object sender, RoutedEventArgs e)
    {
        HidePasswordPanel();
    }

    // ==================== 原有功能 ====================

    private void AddStudent_Click(object sender, RoutedEventArgs e)
    {
        string name = NameInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("请输入学生姓名");
            return;
        }
        _studentService.AddStudent(name);
        NameInput.Text = string.Empty;
    }

    private void RemoveStudent_Click(object sender, RoutedEventArgs e)
    {
        if (StudentList.SelectedItem is StudentInfo selected)
        {
            _studentService.RemoveStudent(selected.Name);
        }
        else
        {
            MessageBox.Show("请先选中要删除的学生");
        }
    }

    private void ShowWidget_Click(object sender, RoutedEventArgs e)
    {
        _widget ??= _serviceProvider.GetService<DesktopWidget>();
        _widget?.Show();
        _widget?.Activate();
    }

    private void ImportList_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog();
        dialog.Filter = "所有支持的文件|*.txt;*.xlsx;*.xls;*.docx|文本文件|*.txt|Excel文件|*.xlsx;*.xls|Word文档|*.docx";
        if (dialog.ShowDialog() != true) return;

        string ext = Path.GetExtension(dialog.FileName).ToLower();
        try
        {
            List<string> names = new();
            switch (ext)
            {
                case ".txt":
                    names.AddRange(File.ReadAllLines(dialog.FileName)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l)));
                    break;
                case ".xlsx":
                case ".xls":
                    MessageBox.Show("Excel导入需安装 NuGet 包「NPOI」，安装后可自动读取第一列姓名");
                    return;
                case ".docx":
                    MessageBox.Show("Word导入需安装 NuGet 包「DocX」，安装后可自动提取段落中的姓名");
                    return;
                default:
                    MessageBox.Show("不支持的文件格式");
                    return;
            }

            int added = 0;
            foreach (var name in names)
            {
                if (!_studentService.Students.Any(s => s.Name == name))
                {
                    _studentService.AddStudent(name);
                    added++;
                }
            }
            MessageBox.Show($"导入完成，成功添加 {added} 名学生");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导入失败：{ex.Message}");
        }
    }
}
