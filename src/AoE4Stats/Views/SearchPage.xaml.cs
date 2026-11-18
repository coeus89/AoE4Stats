using AoE4Stats.Core.Presentation;
using AoE4Stats.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AoE4Stats.Views;

public sealed partial class SearchPage : Page
{
    public SearchPage()
    {
        ViewModel = App.Current.Services.Search;
        InitializeComponent();
    }

    public SearchViewModel ViewModel { get; }

    private void QueryBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == global::Windows.System.VirtualKey.Enter && ViewModel.SearchCommand.CanExecute(null))
        {
            ViewModel.SearchCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void Results_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is PlayerSearchItem item)
        {
            ViewModel.SelectPlayerCommand.Execute(item);
        }
    }
}
