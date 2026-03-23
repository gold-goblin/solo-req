using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using SoloReq.Services;

namespace SoloReq.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Версия {UpdateService.GetCurrentVersion()}";
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private void Link_Click(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://solo-log.ru/projects/solo-req") { UseShellExecute = true });
    }

    private void SettingsFolder_Click(object sender, MouseButtonEventArgs e)
    {
        var dir = PathResolver.GetSettingsDir();
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
    }
}
