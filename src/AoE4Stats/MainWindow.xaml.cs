using AoE4Stats.Core.ViewModels;
using AoE4Stats.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AoE4Stats;

public sealed partial class MainWindow : Window
{
    private long _shownProfileId;

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        TrySetIcon();
        ResizeForDesktop();

        App.Current.Services.Session.Changed += OnSessionChanged;
        if (App.Current.Services.Session.Current is { } selected)
        {
            _shownProfileId = selected.ProfileId;
            Nav.SelectedItem = DashboardItem;
            Navigate("dashboard");
        }
        else
        {
            Nav.SelectedItem = SearchItem;
            Navigate("search");
        }
    }

    public ShellViewModel Shell => App.Current.Services.Shell;

    // x:Bind converters are looked up from a FrameworkElement. Window is not one,
    // so the generated SetConverterLookupRoot(this) call does not compile.
    private Visibility ForgetVisibility(bool hasPlayer) =>
        hasPlayer ? Visibility.Visible : Visibility.Collapsed;

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        Nav.IsPaneOpen = !Nav.IsPaneOpen;
    }

    private void Nav_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        if (tag == "api")
        {
            _ = Windows.System.Launcher.LaunchUriAsync(new Uri("https://aoe4world.com/api"));
            return;
        }

        Navigate(tag);
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var selected = App.Current.Services.Session.Current;
            if (selected is null)
            {
                _shownProfileId = 0;
                Nav.SelectedItem = SearchItem;
                Navigate("search");
                return;
            }

            if (selected.ProfileId == _shownProfileId)
            {
                return;
            }

            _shownProfileId = selected.ProfileId;
            Nav.SelectedItem = DashboardItem;
            Navigate("dashboard");
        });
    }

    private void Navigate(string tag)
    {
        var page = tag switch
        {
            "dashboard" => typeof(DashboardPage),
            "matches" => typeof(MatchesPage),
            "replays" => typeof(ReplayPlaceholderPage),
            _ => typeof(SearchPage),
        };

        if (RootFrame.CurrentSourcePageType == page)
        {
            return;
        }

        RootFrame.Navigate(page);
        RootFrame.BackStack.Clear();
        RootFrame.ForwardStack.Clear();
    }

    private void ResizeForDesktop()
    {
        AppWindow.Resize(new global::Windows.Graphics.SizeInt32(1180, 820));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 960;
            presenter.PreferredMinimumHeight = 640;
        }
    }

    private void TrySetIcon()
    {
        try
        {
            var icon = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(icon))
            {
                AppWindow.SetIcon(icon);
            }
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or InvalidOperationException)
        {
            // The window still opens if the loose layout did not copy the icon.
        }
    }
}
