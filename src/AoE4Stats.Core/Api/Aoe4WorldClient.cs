using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace AoE4Stats.Core.Api;

/// <summary>
/// Typed client for the public AoE4 World read API. No API key.
/// Calls are explicit: this type never polls and never walks every page by itself.
/// </summary>
public sealed class Aoe4WorldClient
{
    public const string BaseUrl = "https://aoe4world.com/";
    public const int MinimumQueryLength = 3;
    public const int DefaultGamesLimit = 20;
    public const int MaxGamesLimit = 25;

    /// <summary>
    /// Identifies this app. AoE4 World asks clients not to spoof a browser user agent.
    /// There is no project email in source control; add one here before a public release.
    /// </summary>
    public const string ProductUserAgent =
        "AoE4Stats/0.1 (Windows desktop; public AoE4 World lookup; non-bulk; +https://aoe4world.com/api)";

    private static readonly HashSet<string> GamesLeaderboards = new(StringComparer.Ordinal)
    {
        "rm_solo",
        "rm_team",
        "rm_1v1",
        "rm_2v2",
        "rm_3v3",
        "rm_4v4",
        "qm_1v1",
        "qm_2v2",
        "qm_3v3",
        "qm_4v4",
    };

    private readonly HttpClient _http;

    public Aoe4WorldClient(HttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(ProductUserAgent);
        }

        if (!_http.DefaultRequestHeaders.Accept.Any())
        {
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    public Task<PlayerSearchPage> SearchPlayersAsync(
        string query,
        int page = 1,
        bool exact = false,
        CancellationToken cancellationToken = default)
    {
        var uri = BuildSearchUri(query, page, exact);
        return SendAsync(uri, Aoe4JsonContext.Default.PlayerSearchPage, cancellationToken);
    }

    public Task<PlayerProfile> GetPlayerAsync(long profileId, CancellationToken cancellationToken = default)
    {
        var uri = BuildProfileUri(profileId);
        return SendAsync(uri, Aoe4JsonContext.Default.PlayerProfile, cancellationToken);
    }

    public Task<PlayerGamesPage> GetPlayerGamesAsync(
        long profileId,
        PlayerGamesRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var uri = BuildGamesUri(profileId, request ?? new PlayerGamesRequest());
        return SendAsync(uri, Aoe4JsonContext.Default.PlayerGamesPage, cancellationToken);
    }

    public static Uri BuildSearchUri(string query, int page, bool exact)
    {
        var trimmed = (query ?? string.Empty).Trim();
        if (trimmed.Length < MinimumQueryLength)
        {
            throw new ArgumentException(
                $"Search text must be at least {MinimumQueryLength} characters.",
                nameof(query));
        }

        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Page starts at 1.");
        }

        var builder = new StringBuilder("https://aoe4world.com/api/v0/players/search?query=");
        builder.Append(Uri.EscapeDataString(trimmed));
        if (page > 1)
        {
            builder.Append("&page=").Append(page.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (exact)
        {
            builder.Append("&exact=true");
        }

        return new Uri(builder.ToString());
    }

    public static Uri BuildProfileUri(long profileId)
    {
        if (profileId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(profileId));
        }

        return new Uri($"https://aoe4world.com/api/v0/players/{profileId.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }

    public static Uri BuildGamesUri(long profileId, PlayerGamesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (profileId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(profileId));
        }

        if (request.Page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Page starts at 1.");
        }

        var limit = Math.Clamp(request.Limit, 1, MaxGamesLimit);
        var builder = new StringBuilder("https://aoe4world.com/api/v0/players/");
        builder.Append(profileId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.Append("/games?limit=");
        builder.Append(limit.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (request.Page > 1)
        {
            builder.Append("&page=").Append(request.Page.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(request.Leaderboard))
        {
            var leaderboard = request.Leaderboard.Trim();
            if (!GamesLeaderboards.Contains(leaderboard))
            {
                throw new ArgumentException(
                    $"Leaderboard '{leaderboard}' is not a documented player-games filter.",
                    nameof(request));
            }

            builder.Append("&leaderboard=").Append(Uri.EscapeDataString(leaderboard));
        }

        if (request.Since is DateTimeOffset since)
        {
            builder.Append("&since=");
            builder.Append(Uri.EscapeDataString(since.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture)));
        }

        return new Uri(builder.ToString());
    }

    private async Task<T> SendAsync<T>(Uri uri, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
        where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        try
        {
            using var response = await _http
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new Aoe4WorldException(response.StatusCode, DescribeStatus(response.StatusCode, body));
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var payload = await JsonSerializer.DeserializeAsync(stream, typeInfo, cancellationToken).ConfigureAwait(false);
            if (payload is null)
            {
                throw new JsonException("AoE4 World returned an empty JSON document.");
            }

            return payload;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new Aoe4WorldException(HttpStatusCode.RequestTimeout, "AoE4 World did not answer in time.");
        }
    }

    private static string DescribeStatus(HttpStatusCode status, string? body)
    {
        var message = status switch
        {
            HttpStatusCode.NotFound => "AoE4 World has no record for that request.",
            HttpStatusCode.BadRequest => "AoE4 World rejected the request.",
            HttpStatusCode.TooManyRequests => "AoE4 World asked this app to slow down. Wait a moment, then try again.",
            _ => $"AoE4 World returned HTTP {(int)status}.",
        };

        var snippet = Sanitize(body);
        return snippet.Length == 0 ? message : message + " " + snippet;
    }

    private static string Sanitize(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        var trimmed = body.Trim().ReplaceLineEndings(" ");
        if (trimmed.Length > 180)
        {
            trimmed = trimmed[..180];
        }

        return trimmed;
    }
}
