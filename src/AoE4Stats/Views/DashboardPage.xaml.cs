using AoE4Stats.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AoE4Stats.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardPage()
    {
        ViewModel = App.Current.Services.Dashboard;
        InitializeComponent();
    }

    public DashboardViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (ViewModel.LoadCommand.CanExecute(null))
        {
            ViewModel.LoadCommand.Execute(null);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.LoadCommand.Cancel();
        ViewModel.RefreshCommand.Cancel();
        base.OnNavigatedFrom(e);
    }

    private void OpenProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TryGetProfileUri(out var uri) && uri is not null)
        {
            _ = Windows.System.Launcher.LaunchUriAsync(uri);
        }
    }
}
