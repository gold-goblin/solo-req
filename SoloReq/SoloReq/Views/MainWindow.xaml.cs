using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using SoloReq.Services;
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

        InputBindings.Add(new KeyBinding(
            new RelayCommand(() => ResponsePanelControl.OpenSearch()),
            Key.F, ModifierKeys.Control));

        // Set request panel's DataContext
        RequestPanelControl.DataContext = _viewModel.Request;

        Loaded += async (_, _) =>
        {
            RestorePanelProportions();
            _viewModel.History.Initialize();
            UpdateService.CleanupOldVersion();
            _ = _viewModel.UserAgentService.LoadAsync();
            await _viewModel.Update.CheckForUpdateCommand.ExecuteAsync(null);
        };

        Closing += (_, _) => SavePanelProportions();
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

    private void RestorePanelProportions()
    {
        var settings = _viewModel.SettingsService.Load();
        if (settings == null) return;

        // Restore history panel width
        var historyWidth = Math.Max(160, Math.Min(350, settings.HistoryPanelWidth));
        MainContentGrid.ColumnDefinitions[0].Width = new GridLength(historyWidth);

        // Restore request/response ratio
        var ratio = Math.Max(0.1, Math.Min(0.9, settings.RequestPanelRatio));
        RequestResponseGrid.RowDefinitions[0].Height = new GridLength(ratio, GridUnitType.Star);
        RequestResponseGrid.RowDefinitions[2].Height = new GridLength(1 - ratio, GridUnitType.Star);
    }

    private void SavePanelProportions()
    {
        var settings = _viewModel.SettingsService.Load() ?? new AppSettings();

        settings.HistoryPanelWidth = MainContentGrid.ColumnDefinitions[0].ActualWidth;

        var requestHeight = RequestResponseGrid.RowDefinitions[0].ActualHeight;
        var responseHeight = RequestResponseGrid.RowDefinitions[2].ActualHeight;
        var total = requestHeight + responseHeight;
        if (total > 0)
            settings.RequestPanelRatio = requestHeight / total;

        _viewModel.SettingsService.Save(settings);
    }
}
