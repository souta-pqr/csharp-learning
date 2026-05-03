using System.Windows;

namespace _03_wpf_app;

public partial class MainWindow : Window
{
    private int _count = 0;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        _count++;
        ResultText.Text = $"{_count} 回クリックされました！";
    }
}
