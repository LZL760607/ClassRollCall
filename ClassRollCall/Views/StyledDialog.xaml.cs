using System.Windows;
using System.Windows.Media.Animation;

namespace ClassRollCall.Views;

public partial class StyledDialog : Window
{
    public bool Confirmed { get; private set; }

    public StyledDialog(string title, string content,
                        string confirmText = "确 定",
                        string cancelText = "取 消")
    {
        InitializeComponent();

        TitleText.Text = title;
        ContentText.Text = content;
        ConfirmButton.Content = confirmText;
        CancelButton.Content = cancelText;

        Loaded += (s, e) =>
        {
            ((Storyboard)FindResource("FadeInStoryboard")).Begin();
        };
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }
}
