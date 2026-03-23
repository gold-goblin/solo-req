using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SoloReq.Models;
using SoloReq.ViewModels;

namespace SoloReq.Views;

public partial class UserAgentPickerWindow : Window
{
    public UserAgentPickerWindow()
    {
        InitializeComponent();
    }

    public UserAgentEntry? SelectedUserAgent =>
        (DataContext as UserAgentPickerViewModel)?.SelectedEntry;

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private void FilterAll_Click(object sender, RoutedEventArgs e) =>
        SetFilter("Все");

    private void FilterDesktop_Click(object sender, RoutedEventArgs e) =>
        SetFilter("Desktop");

    private void FilterMobile_Click(object sender, RoutedEventArgs e) =>
        SetFilter("Mobile");

    private void FilterCustom_Click(object sender, RoutedEventArgs e) =>
        SetFilter("Пользовательские");

    private void SetFilter(string filter)
    {
        if (DataContext is UserAgentPickerViewModel vm)
            vm.SelectedDeviceFilter = filter;

        BtnFilterAll.Tag = filter == "Все" ? "Active" : null;
        BtnFilterDesktop.Tag = filter == "Desktop" ? "Active" : null;
        BtnFilterMobile.Tag = filter == "Mobile" ? "Active" : null;
        BtnFilterCustom.Tag = filter == "Пользовательские" ? "Active" : null;
    }

    private void AddCustom_Click(object sender, RoutedEventArgs e)
    {
        var editorVm = new UserAgentEditorViewModel();
        var editor = new UserAgentEditorWindow
        {
            DataContext = editorVm,
            Owner = this
        };

        if (editor.ShowDialog() == true)
        {
            if (DataContext is UserAgentPickerViewModel vm)
                vm.AddCustomEntry(editorVm.ToEntry());
        }
    }

    private void EditCustom_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: UserAgentEntry entry })
            return;

        var editorVm = new UserAgentEditorViewModel(entry);
        var editor = new UserAgentEditorWindow
        {
            DataContext = editorVm,
            Owner = this
        };

        if (editor.ShowDialog() == true)
        {
            if (DataContext is UserAgentPickerViewModel vm)
            {
                if (entry.IsCustom)
                    vm.EditCustomEntry(entry, editorVm.ToEntry());
                else
                    vm.AddCustomEntry(editorVm.ToEntry());
            }
        }
    }

    private void DeleteCustom_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: UserAgentEntry entry })
            return;

        if (DataContext is UserAgentPickerViewModel vm)
            vm.DeleteCustomEntry(entry);
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SelectedUserAgent != null)
        {
            DialogResult = true;
            Close();
        }
    }

    private void DataGrid_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && SelectedUserAgent != null)
        {
            DialogResult = true;
            Close();
        }
    }
}
