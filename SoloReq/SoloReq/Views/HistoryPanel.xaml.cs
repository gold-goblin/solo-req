using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SoloReq.Models;
using SoloReq.ViewModels;

namespace SoloReq.Views;

public partial class HistoryPanel : UserControl
{
    public HistoryPanel()
    {
        InitializeComponent();
    }

    private HistoryViewModel? Vm => DataContext as HistoryViewModel;

    private void HistoryEntry_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is HistoryItem item && Vm != null)
        {
            Vm.SelectItem(item);
        }
    }

    private void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is FrameworkElement fe && fe.DataContext is HistoryItem item)
            Vm?.StartRenamingCommand.Execute(item);
    }

    private void RenameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not HistoryItem item) return;

        if (e.Key == Key.Enter)
        {
            Vm?.FinishRenamingCommand.Execute(item);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Vm?.CancelRenamingCommand.Execute(item);
            e.Handled = true;
        }
    }

    private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is HistoryItem item && item.IsEditing)
            Vm?.FinishRenamingCommand.Execute(item);
    }

    private void RenameTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.Focus();
            tb.SelectAll();
        }
    }
}
