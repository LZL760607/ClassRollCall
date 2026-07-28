using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
            _studentService.RemoveStudent(selected.Name);
        else
            MessageBox.Show("请先选中要删除的学生");
    }

    private void ShowWidget_Click(object sender, RoutedEventArgs e)
    {
        _widget ??= _serviceProvider.GetService<DesktopWidget>();
        _widget?.Show();
        _widget?.Activate();
    }

    private void OpenWeight_Click(object sender, RoutedEventArgs e)
    {
        _weightWindow ??= _serviceProvider.GetService<WeightManageWindow>();
        _weightWindow?.Show();
        _weightWindow?.Activate();
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
