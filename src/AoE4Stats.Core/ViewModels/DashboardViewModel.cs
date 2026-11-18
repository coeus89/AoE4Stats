using System.Collections.ObjectModel;
using System.Text.Json;
using AoE4Stats.Core.Api;
using AoE4Stats.Core.Formatting;
using AoE4Stats.Core.Presentation;
using AoE4Stats.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AoE4Stats.Core.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly Aoe4WorldLookup _lookup;
    private readonly PlayerSession _session;
    private int _generation;

    public DashboardViewModel(Aoe4WorldLookup lookup, PlayerSession session)
    {
        ArgumentNullException.ThrowIfNull(lookup);
        ArgumentNullException.ThrowIfNull(session);
        _lookup = lookup;
        _session = session;
        PrimaryCards = new ObservableCollection<ModeCard>();
        LifetimeCards = new ObservableCollection<ModeCard>();
    }

    public ObservableCollection<ModeCard> PrimaryCards { get; }

    public ObservableCollection<ModeCard> LifetimeCards { get; }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private string _playerName = "";

    [ObservableProperty]
    private string _country = "";

    [ObservableProperty]
    private string _freshness = "";

    [ObservableProperty]
    private string? _profileUrl;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool NeedsPlayer => _session.Current is null && !IsLoading;

    public bool HasPrimary => PrimaryCards.Count > 0;

    public bool HasLifetime => LifetimeCards.Count > 0;

    public bool ShowEmptyModes => _session.Current is not null && !IsLoading && !HasError && !HasPrimary && !HasLifetime;

    public bool HasProfileLink => TryGetProfileUri(out _);

    [RelayCommand]
    private Task LoadAsync(CancellationToken cancellationToken) => LoadCoreAsync(forceRefresh: false, cancellationToken);

    [RelayCommand]
    private Task RefreshAsync(CancellationToken cancellationToken) => LoadCoreAsync(forceRefresh: true, cancellationToken);

    public bool TryGetProfileUri(out Uri? uri) => AoE4Display.TryGetAoe4WorldUri(ProfileUrl, out uri);

    private async Task LoadCoreAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        var selected = _session.Current;
        if (selected is null)
        {
            Clear();
            PlayerName = "";
            Country = "";
            Freshness = "Choose a player from Search. Ratings are loaded from AoE4 World, not invented.";
            Notify();
            return;
        }

        var generation = Interlocked.Increment(ref _generation);
        IsLoading = true;
        ErrorMessage = "";
        ProfileUrl = null;
        PlayerName = selected.Name;
        Country = AoE4Display.FormatCountry(selected.Country);
        try
        {
            var result = await _lookup.GetProfileAsync(selected.ProfileId, forceRefresh, cancellationToken).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            if (generation != _generation)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.Value.Name))
            {
                PlayerName = result.Value.Name.Trim();
            }

            Country = AoE4Display.FormatCountry(result.Value.Country);
            ProfileUrl = result.Value.SiteUrl;
            Freshness = AoE4Display.FormatFreshness(result.FetchedAt, result.FromCache, result.QuietRefresh);
            Replace(PrimaryCards, ModeCardBuilder.BuildPrimary(result.Value.Modes));
            Replace(LifetimeCards, ModeCardBuilder.BuildLifetime(result.Value.Modes));
            _session.RememberDetails(result.Value);
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
            ErrorMessage = "Could not reach AoE4 World. The last saved player is still selected.";
        }
        catch (JsonException)
        {
            ErrorMessage = "AoE4 World returned a profile this version could not read.";
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

    private void Clear()
    {
        PrimaryCards.Clear();
        LifetimeCards.Clear();
        ProfileUrl = null;
        ErrorMessage = "";
    }

    private void Notify()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(NeedsPlayer));
        OnPropertyChanged(nameof(HasPrimary));
        OnPropertyChanged(nameof(HasLifetime));
        OnPropertyChanged(nameof(ShowEmptyModes));
        OnPropertyChanged(nameof(HasProfileLink));
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}
