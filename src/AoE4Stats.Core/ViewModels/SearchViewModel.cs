using System.Collections.ObjectModel;
using System.Text.Json;
using AoE4Stats.Core.Api;
using AoE4Stats.Core.Formatting;
using AoE4Stats.Core.Presentation;
using AoE4Stats.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AoE4Stats.Core.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly Aoe4WorldLookup _lookup;
    private readonly PlayerSession _session;
    private string _submittedQuery = "";
    private int? _nextPage;
    private int _page = 1;
    private int _generation;

    public SearchViewModel(Aoe4WorldLookup lookup, PlayerSession session)
    {
        ArgumentNullException.ThrowIfNull(lookup);
        ArgumentNullException.ThrowIfNull(session);
        _lookup = lookup;
        _session = session;
        Results = new ObservableCollection<PlayerSearchItem>();
    }

    public ObservableCollection<PlayerSearchItem> Results { get; }

    public int MinimumQueryLength => Aoe4WorldClient.MinimumQueryLength;

    [ObservableProperty]
    private string _query = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasSearched;

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private string _hint = "Search the public AoE4 World directory. Nothing is sent until you search.";

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private bool _canGoNext;

    [ObservableProperty]
    private bool _canGoPrevious;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool ShowEmpty => HasSearched && !IsLoading && !HasError && Results.Count == 0;

    public bool HasResults => Results.Count > 0;

    private bool CanSearch() => Query.Trim().Length >= Aoe4WorldClient.MinimumQueryLength;

    partial void OnQueryChanged(string value)
    {
        SearchCommand.NotifyCanExecuteChanged();
        if (!HasSearched)
        {
            Hint = value.Trim().Length is > 0 and < Aoe4WorldClient.MinimumQueryLength
                ? "Enter at least 3 characters."
                : "Search the public AoE4 World directory. Nothing is sent until you search.";
        }
    }

    partial void OnErrorMessageChanged(string value) => NotifyLists();

    partial void OnIsLoadingChanged(bool value)
    {
        NotifyLists();
        NextPageCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnHasSearchedChanged(bool value) => NotifyLists();

    [RelayCommand(CanExecute = nameof(CanSearch), IncludeCancelCommand = true)]
    private Task SearchAsync(CancellationToken cancellationToken)
    {
        _submittedQuery = Query.Trim();
        return RunAsync(1, cancellationToken);
    }

    private bool CanGoNextPage() => CanGoNext && !IsLoading;

    private bool CanGoPreviousPage() => CanGoPrevious && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanGoNextPage))]
    private Task NextPageAsync(CancellationToken cancellationToken) =>
        RunAsync(_page + 1, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanGoPreviousPage))]
    private Task PreviousPageAsync(CancellationToken cancellationToken) =>
        RunAsync(Math.Max(1, _page - 1), cancellationToken);

    [RelayCommand]
    private void SelectPlayer(PlayerSearchItem? item)
    {
        if (item is null)
        {
            return;
        }

        _session.Select(new PlayerSummary
        {
            ProfileId = item.ProfileId,
            Name = item.Name,
            Country = item.Country == "—" ? null : item.CountryCode,
        });
    }

    private async Task RunAsync(int page, CancellationToken cancellationToken)
    {
        if (_submittedQuery.Length < Aoe4WorldClient.MinimumQueryLength)
        {
            return;
        }

        var generation = Interlocked.Increment(ref _generation);
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var result = await _lookup.SearchAsync(_submittedQuery, page, exact: false, forceRefresh: false, cancellationToken)
                .ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            if (generation != _generation)
            {
                return;
            }

            Apply(result, page);
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
            ErrorMessage = "Could not reach AoE4 World. Check the connection and try again.";
        }
        catch (JsonException)
        {
            ErrorMessage = "AoE4 World returned player search data this version could not read.";
        }
        finally
        {
            if (generation == _generation)
            {
                IsLoading = false;
                HasSearched = true;
            }
        }
    }

    private void Apply(LookupResult<PlayerSearchPage> result, int page)
    {
        _page = result.Value.Page ?? page;
        _nextPage = result.Value.NextPage;
        CanGoNext = _nextPage is not null;
        CanGoPrevious = _page > 1;
        NextPageCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();

        Results.Clear();
        foreach (var player in result.Value.Players ?? [])
        {
            Results.Add(ToItem(player));
        }

        var shown = result.Value.Count ?? Results.Count;
        var total = result.Value.TotalCount;
        Status = total is int all
            ? $"Page {_page}. Showing {shown} of {all}. {AoE4Display.FormatFreshness(result.FetchedAt, result.FromCache, result.QuietRefresh)}"
            : $"Page {_page}. {AoE4Display.FormatFreshness(result.FetchedAt, result.FromCache, result.QuietRefresh)}";
        NotifyLists();
    }

    private static PlayerSearchItem ToItem(PlayerSummary player)
    {
        var solo = FindHeadline(player.Leaderboards);
        var summary = solo is null
            ? "No ladder snapshot in this search row."
            : $"{AoE4Display.FormatMode(solo.Value.Key)} · {AoE4Display.FormatRating(solo.Value.Stats.Rating)} · {AoE4Display.FormatRankLevel(solo.Value.Stats.RankLevel)} · {AoE4Display.FormatWinRate(solo.Value.Stats.WinRate, solo.Value.Stats.WinsCount, solo.Value.Stats.LossesCount)}";

        Uri? avatar = null;
        if (Uri.TryCreate(player.Avatars?.Small, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
        {
            avatar = uri;
        }

        return new PlayerSearchItem
        {
            Name = string.IsNullOrWhiteSpace(player.Name) ? $"Profile {player.ProfileId}" : player.Name.Trim(),
            ProfileId = player.ProfileId,
            IdText = player.ProfileId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Country = AoE4Display.FormatCountry(player.Country),
            CountryCode = player.Country,
            Summary = summary,
            LastGame = AoE4Display.FormatTimestamp(player.LastGameAt),
            AvatarUri = avatar,
        };
    }

    private static (string Key, ModeStats Stats)? FindHeadline(Dictionary<string, ModeStats>? boards)
    {
        if (boards is null)
        {
            return null;
        }

        foreach (var key in new[] { "rm_solo", "rm_team", "qm_1v1", "rm_1v1" })
        {
            if (boards.TryGetValue(key, out var stats) && (stats.Rating is not null || (stats.GamesCount ?? 0) > 0))
            {
                return (key, stats);
            }
        }

        return null;
    }

    private void NotifyLists()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ShowEmpty));
        OnPropertyChanged(nameof(HasResults));
    }
}
