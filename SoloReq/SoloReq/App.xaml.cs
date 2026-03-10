using System;
using System.Windows;

namespace SoloReq;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show(args.Exception.ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            MessageBox.Show(args.ExceptionObject.ToString(), "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        };
        base.OnStartup(e);
    }
}
