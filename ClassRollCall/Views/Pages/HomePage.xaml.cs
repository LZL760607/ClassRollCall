using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClassRollCall.Services;
using ClassRollCall.Views;

namespace ClassRollCall.Views.Pages;

public partial class HomePage : Page
{
    private readonly StudentService _studentService;

    public HomePage(StudentService studentService)
    {
        _studentService = studentService;
        InitializeComponent();
    }

    private void StartRoll_Click(object sender, RoutedEventArgs e)
    {
        if (_studentService.Students.Count < 2)
        {
            MessageBox.Show("请先在学生管理中添加至少2名学生");
            return;
        }

        int count = _studentService.RollCount;
        var results = _studentService.RollRandomStudents(count);
        var names = results.Select(s => s.Name).ToList();

        var popup = new ResultPopupWindow(names);
        popup.Show();
    }
}
