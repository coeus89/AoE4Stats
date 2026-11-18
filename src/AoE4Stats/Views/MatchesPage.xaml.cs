using AoE4Stats.Core.Presentation;
using AoE4Stats.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AoE4Stats.Views;

public sealed partial class MatchesPage : Page
{
    private bool _ready;

    public MatchesPage()
    {
        ViewModel = App.Current.Services.Matches;
        InitializeComponent();
    }

    public MatchesViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ready = true;
        if (ViewModel.LoadCommand.CanExecute(null))
        {
            ViewModel.LoadCommand.Execute(null);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _ready = false;
        base.OnNavigatedFrom(e);
    }

    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || e.AddedItems.Count == 0 || e.AddedItems[0] is not ModeFilter filter)
        {
            return;
        }

        ViewModel.SelectedFilter = filter;

        if (ViewModel.LoadCommand.CanExecute(null))
        {
            ViewModel.LoadCommand.Execute(null);
        }
    }
}
