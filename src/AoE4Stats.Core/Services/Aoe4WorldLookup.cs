using AoE4Stats.Core.Api;
using AoE4Stats.Core.Caching;

namespace AoE4Stats.Core.Services;

public readonly record struct LookupResult<T>(
    T Value,
    DateTimeOffset FetchedAt,
    bool FromCache,
    bool QuietRefresh);

/// <summary>
/// Adds short TTLs and a refresh quiet period in front of <see cref="Aoe4WorldClient"/>.
/// A refresh click inside the quiet period reuses the last payload instead of calling the API again.
/// </summary>
public sealed class Aoe4WorldLookup
{
    public static readonly TimeSpan SearchTtl = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan ProfileTtl = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan GamesTtl = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan RefreshQuietPeriod = TimeSpan.FromSeconds(15);

    private readonly Aoe4WorldClient _client;
    private readonly TtlMemoryCache _cache;
    private readonly IClock _clock;

    public Aoe4WorldLookup(Aoe4WorldClient client, TtlMemoryCache cache, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(clock);
        _client = client;
        _cache = cache;
        _clock = clock;
    }

    public Task<LookupResult<PlayerSearchPage>> SearchAsync(
        string query,
        int page,
        bool exact,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var key = $"search:{query.Trim()}|{page.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{exact}";
        return GetAsync(
            key,
            SearchTtl,
            forceRefresh,
            token => _client.SearchPlayersAsync(query, page, exact, token),
            cancellationToken);
    }

    public Task<LookupResult<PlayerProfile>> GetProfileAsync(
        long profileId,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var key = $"profile:{profileId.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return GetAsync(
            key,
            ProfileTtl,
            forceRefresh,
            token => _client.GetPlayerAsync(profileId, token),
            cancellationToken);
    }

    public Task<LookupResult<PlayerGamesPage>> GetGamesAsync(
        long profileId,
        PlayerGamesRequest request,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var leaderboard = request.Leaderboard ?? "all";
        var since = request.Since?.ToUniversalTime().ToString("o", System.Globalization.CultureInfo.InvariantCulture) ?? "any";
        var key =
            $"games:{profileId.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{leaderboard}|{request.Page}|{request.Limit}|{since}";
        return GetAsync(
            key,
            GamesTtl,
            forceRefresh,
            token => _client.GetPlayerGamesAsync(profileId, request, token),
            cancellationToken);
    }

    private async Task<LookupResult<T>> GetAsync<T>(
        string key,
        TimeSpan ttl,
        bool forceRefresh,
        Func<CancellationToken, Task<T>> fetch,
        CancellationToken cancellationToken)
        where T : class
    {
        if (_cache.TryGet<T>(key, out var cached, out var storedAt) && cached is not null)
        {
            var age = _clock.UtcNow - storedAt;
            if (!forceRefresh || age < RefreshQuietPeriod)
            {
                return new LookupResult<T>(cached, storedAt, true, forceRefresh);
            }
        }

        var value = await fetch(cancellationToken).ConfigureAwait(false);
        var now = _clock.UtcNow;
        _cache.Set(key, value, ttl);
        return new LookupResult<T>(value, now, false, false);
    }
}
