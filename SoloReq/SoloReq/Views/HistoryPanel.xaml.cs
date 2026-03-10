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

    private void HistoryItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is HistoryItem item
            && DataContext is HistoryViewModel vm)
        {
            vm.SelectedItem = item;
        }
    }
}
