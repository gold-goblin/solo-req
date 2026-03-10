using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace SoloReq.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private void Link_Click(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://solo-log.ru/projects/solo-req") { UseShellExecute = true });
    }
}
