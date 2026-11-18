using System.Collections.ObjectModel;
using System.Text.Json;
using AoE4Stats.Core.Api;
using AoE4Stats.Core.Formatting;
using AoE4Stats.Core.Presentation;
using AoE4Stats.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AoE4Stats.Core.ViewModels;

public partial class MatchesViewModel : ObservableObject
{
    public static IReadOnlyList<ModeFilter> FilterChoices { get; } =
    [
        new() { Label = "All recent", Leaderboard = null },
        new() { Label = "Ranked 1v1", Leaderboard = "rm_solo" },
        new() { Label = "Ranked team", Leaderboard = "rm_team" },
        new() { Label = "Quick match 1v1", Leaderboard = "qm_1v1" },
        new() { Label = "Quick match 2v2", Leaderboard = "qm_2v2" },
        new() { Label = "Quick match 3v3", Leaderboard = "qm_3v3" },
        new() { Label = "Quick match 4v4", Leaderboard = "qm_4v4" },
    ];

    public IReadOnlyList<ModeFilter> Filters => FilterChoices;

    private readonly Aoe4WorldLookup _lookup;
    private readonly PlayerSession _session;
    private int _page = 1;
    private int? _nextPage;
    private int _generation;

    public MatchesViewModel(Aoe4WorldLookup lookup, PlayerSession session)
    {
        ArgumentNullException.ThrowIfNull(lookup);
        ArgumentNullException.ThrowIfNull(session);
        _lookup = lookup;
        _session = session;
        Rows = new ObservableCollection<MatchRow>();
        _selectedFilter = FilterChoices[0];
    }

    public ObservableCollection<MatchRow> Rows { get; }

    [ObservableProperty]
    private ModeFilter _selectedFilter;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private string _freshness = "";

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private bool _canGoNext;

    [ObservableProperty]
    private bool _canGoPrevious;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool NeedsPlayer => _session.Current is null && !IsLoading;

    public bool ShowEmpty => _session.Current is not null && !IsLoading && !HasError && Rows.Count == 0;

    public bool HasRows => Rows.Count > 0;

    private bool CanGoNextPage() => CanGoNext && !IsLoading;

    private bool CanGoPreviousPage() => CanGoPrevious && !IsLoading;

    [RelayCommand]
    private Task LoadAsync(CancellationToken cancellationToken)
    {
        _page = 1;
        return LoadCoreAsync(forceRefresh: false, cancellationToken);
    }

    [RelayCommand]
    private Task RefreshAsync(CancellationToken cancellationToken)
    {
        _page = 1;
        return LoadCoreAsync(forceRefresh: true, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoNextPage))]
    private Task NextPageAsync(CancellationToken cancellationToken)
    {
        _page++;
        return LoadCoreAsync(forceRefresh: false, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoPreviousPage))]
    private Task PreviousPageAsync(CancellationToken cancellationToken)
    {
        _page = Math.Max(1, _page - 1);
        return LoadCoreAsync(forceRefresh: false, cancellationToken);
    }

    private async Task LoadCoreAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        var selected = _session.Current;
        if (selected is null)
        {
            Rows.Clear();
            ErrorMessage = "";
            Freshness = "";
            Status = "Choose a player from Search to load recent matches.";
            CanGoNext = false;
            CanGoPrevious = false;
            Notify();
            return;
        }

        var generation = Interlocked.Increment(ref _generation);
        IsLoading = true;
            ErrorMessage = "";
        try
        {
            var request = new PlayerGamesRequest
            {
                Page = _page,
                Limit = Aoe4WorldClient.DefaultGamesLimit,
                Leaderboard = SelectedFilter.Leaderboard,
            };
            var result = await _lookup.GetGamesAsync(selected.ProfileId, request, forceRefresh, cancellationToken)
                .ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            if (generation != _generation)
            {
                return;
            }

            _page = result.Value.Page ?? _page;
            _nextPage = result.Value.NextPage;
            CanGoNext = _nextPage is not null;
            CanGoPrevious = _page > 1;
            Rows.Clear();
            foreach (var row in MatchRowBuilder.Build(result.Value.Games, selected.ProfileId))
            {
                Rows.Add(row);
            }

            var total = result.Value.TotalCount;
            Status = total is int all
                ? $"Page {_page}. {Rows.Count} matches on this page, {all:N0} in the filter."
                : $"Page {_page}. {Rows.Count} matches on this page.";
            Freshness = AoE4Display.FormatFreshness(result.FetchedAt, result.FromCache, result.QuietRefresh);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Aoe4WorldException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not reach AoE4 World.";
        }
        catch (JsonException)
        {
            ErrorMessage = "AoE4 World returned match data this version could not read.";
        }
        finally
        {
            if (generation == _generation)
            {
                IsLoading = false;
                Notify();
            }
        }
    }

    partial void OnIsLoadingChanged(bool value) => NotifyPaging();

    partial void OnSelectedFilterChanged(ModeFilter value)
    {
        _page = 1;
    }

    private void Notify()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(NeedsPlayer));
        OnPropertyChanged(nameof(ShowEmpty));
        OnPropertyChanged(nameof(HasRows));
        NotifyPaging();
    }

    private void NotifyPaging()
    {
        NextPageCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();
    }
}
