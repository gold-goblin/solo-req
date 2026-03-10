using System.Windows;
using CommunityToolkit.Mvvm.Input;
using SoloReq.ViewModels;

namespace SoloReq.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        _viewModel = new MainViewModel();
        _viewModel.OpenCookieManagerCommand = new RelayCommand(OpenCookieManager);
        _viewModel.OpenAboutCommand = new RelayCommand(OpenAbout);

        if (_viewModel.IsFirstRun)
        {
            _viewModel.ApplyTheme(false); // Light theme for picker
            var picker = new ThemePickerWindow();
            picker.ShowDialog();
            _viewModel.ApplyTheme(picker.SelectedDarkTheme);
        }

        DataContext = _viewModel;
        InitializeComponent();

        // Commands that need UI elements created by InitializeComponent
        _viewModel.FocusUrlCommand = new RelayCommand(() => RequestPanelControl.FocusUrl());
        _viewModel.FocusHistoryCommand = new RelayCommand(() => { });

        // Set request panel's DataContext
        RequestPanelControl.DataContext = _viewModel.Request;
    }

    private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private void OpenCookieManager()
    {
        _viewModel.CookieManager.Refresh();
        var window = new CookieManagerWindow
        {
            DataContext = _viewModel.CookieManager,
            Owner = this
        };
        window.ShowDialog();
    }

    private void OpenAbout()
    {
        var window = new AboutWindow { Owner = this };
        window.ShowDialog();
    }
}
