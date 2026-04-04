using BrowserAptor.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BrowserAptor.Views;

/// <summary>
/// The main browser and profile selector window shown to the user when a URL
/// is opened via BrowserAptor.
/// </summary>
public partial class BrowserSelectorWindow : Window
{
    public BrowserSelectorWindow(BrowserSelectorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += Close;

        // Cap the window height so it never exceeds the visible screen.
        const double WindowHeightMargin = 60;
        MaxHeight = SystemParameters.PrimaryScreenHeight - WindowHeightMargin;
    }

    private void BrowserList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is BrowserSelectorViewModel vm && vm.OpenCommand.CanExecute(null))
            vm.OpenCommand.Execute(null);
    }

    private void BrowserList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Only act on single-click when single-click mode is enabled and the click
        // landed on an item (not on the empty area of the list).
        if (DataContext is not BrowserSelectorViewModel vm) return;
        if (!vm.SingleClickToOpen) return;

        // Verify the click target is inside a ListBoxItem
        if (e.OriginalSource is DependencyObject source &&
            ItemsControl.ContainerFromElement(BrowserList, source) is ListBoxItem)
        {
            if (vm.OpenCommand.CanExecute(null))
                vm.OpenCommand.Execute(null);
        }
    }

    private void BrowserList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return || e.Key == Key.Enter)
        {
            if (DataContext is BrowserSelectorViewModel vm && vm.OpenCommand.CanExecute(null))
            {
                vm.OpenCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
