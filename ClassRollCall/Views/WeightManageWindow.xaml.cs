using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClassRollCall.Services;

namespace ClassRollCall.Views;

public partial class WeightManageWindow : Window
{
    private readonly StudentService _studentService;

    public double MaxWeight => _studentService.Students.Count;

    public WeightManageWindow(StudentService studentService)
    {
        _studentService = studentService;
        InitializeComponent();
        Loaded += (s, e) => WeightList.ItemsSource = _studentService.Students;

        Closing += (s, e) => { e.Cancel = true; Hide(); };
        PreviewMouseLeftButtonUp += OnWindowMouseUp;
    }

    private void OnWindowMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (IsSliderDragSource(e.OriginalSource))
        {
            Dispatcher.BeginInvoke(() =>
            {
                _studentService.NormalizeWeights();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    private static bool IsSliderDragSource(object? source)
    {
        while (source != null)
        {
            if (source is Slider) return true;
            source = (source as DependencyObject) is FrameworkElement fe
                ? fe.Parent ?? fe.TemplatedParent : null;
        }
        return false;
    }

    private void ToggleLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string name)
            _studentService.ToggleLock(name);
    }

    private void ResetAll_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定要重置所有权重为 1.0 吗？\n（锁定的学生不受影响）",
            "确认重置",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.OK)
            _studentService.ResetAllWeights();
    }
}
