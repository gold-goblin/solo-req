using System.Windows;
using SoloReq.ViewModels;

namespace SoloReq.Views;

public partial class UserAgentEditorWindow : Window
{
    public UserAgentEditorWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is UserAgentEditorViewModel vm)
                TitleText.Text = vm.IsEditMode ? " Редактировать User-Agent" : " Добавить User-Agent";
        };
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();
    private void CancelClick(object sender, RoutedEventArgs e) => Close();

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserAgentEditorViewModel vm && vm.CanSave)
        {
            DialogResult = true;
            Close();
        }
    }
}
