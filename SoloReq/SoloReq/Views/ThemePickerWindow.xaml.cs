using System.Windows;
using System.Windows.Input;

namespace SoloReq.Views;

public partial class ThemePickerWindow : Window
{
    public bool SelectedDarkTheme { get; private set; } = false;

    public ThemePickerWindow()
    {
        InitializeComponent();
    }

    private void DarkTheme_Click(object sender, MouseButtonEventArgs e)
    {
        SelectedDarkTheme = true;
        DialogResult = true;
    }

    private void LightTheme_Click(object sender, MouseButtonEventArgs e)
    {
        SelectedDarkTheme = false;
        DialogResult = true;
    }
}
