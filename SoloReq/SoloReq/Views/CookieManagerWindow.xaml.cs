using System.Windows;

namespace SoloReq.Views;

public partial class CookieManagerWindow : Window
{
    public CookieManagerWindow()
    {
        InitializeComponent();
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();
}
