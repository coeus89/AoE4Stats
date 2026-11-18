using System.Net;
using AoE4Stats.Core.Api;
using AoE4Stats.Core.Caching;
using AoE4Stats.Core.Formatting;
using AoE4Stats.Core.Presentation;
using AoE4Stats.Core.Services;
using AoE4Stats.Core.Storage;
using AoE4Stats.Core.ViewModels;
using Xunit;

namespace AoE4Stats.Core.Tests;

public sealed class CoreBehaviorTests
{
    [Fact]
    public void Search_uri_encodes_query_and_rejects_short_text()
    {
        var uri = Aoe4WorldClient.BuildSearchUri("beasty qt", 2, exact: true);
        Assert.Equal(
            "https://aoe4world.com/api/v0/players/search?query=beasty%20qt&page=2&exact=true",
            uri.AbsoluteUri);
        Assert.Throws<ArgumentException>(() => Aoe4WorldClient.BuildSearchUri("ab", 1, false));
    }

    [Fact]
    public void Games_uri_clamps_limit_and_rejects_unknown_ladder()
    {
        var uri = Aoe4WorldClient.BuildGamesUri(8139502, new PlayerGamesRequest
        {
            Limit = 500,
            Leaderboard = "rm_solo",
            Page = 1,
        });
        Assert.Contains("/api/v0/players/8139502/games?limit=25", uri.AbsoluteUri, StringComparison.Ordinal);
        Assert.Contains("leaderboard=rm_solo", uri.AbsoluteUri, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => Aoe4WorldClient.BuildGamesUri(5, new PlayerGamesRequest
        {
            Leaderboard = "not_a_ladder",
        }));
    }

    [Fact]
    public async Task Client_sends_identifying_user_agent_and_reads_search_fixture()
    {
        var handler = new StubHandler
        {
            Responder = (_, _) => JsonResponse(ReadFixture("search.json")),
        };
        using var http = new HttpClient(handler);
        var client = new Aoe4WorldClient(http);
        var page = await client.SearchPlayersAsync("beasty");

        Assert.Single(handler.Calls);
        Assert.Contains("AoE4Stats/0.1", handler.Calls[0].UserAgent, StringComparison.Ordinal);
        Assert.DoesNotContain("Mozilla", handler.Calls[0].UserAgent, StringComparison.OrdinalIgnoreCase);
        var player = Assert.Single(page.Players!);
        var boards = player.Leaderboards;
        Assert.NotNull(boards);
        Assert.Equal(8139502, player.ProfileId);
        Assert.Equal(1729, boards["rm_solo"].Rating);
        Assert.Null(boards["rm_team"].Rating);
        Assert.Equal("unranked", boards["rm_team"].RankLevel);
    }

    [Fact]
    public async Task Profile_fixture_ignores_rating_history_and_keeps_nulls()
    {
        var handler = new StubHandler { Responder = (_, _) => JsonResponse(ReadFixture("profile.json")) };
        using var http = new HttpClient(handler);
        var profile = await new Aoe4WorldClient(http).GetPlayerAsync(8139502);

        Assert.Equal("http://aoe4world.com/players/8139502", profile.SiteUrl);
        Assert.Equal(32, profile.Modes!["rm_solo"].Civilizations![0].GamesCount);
        Assert.Equal(78.125, profile.Modes["rm_solo"].Civilizations![0].WinRate);
        Assert.Null(profile.Modes["rm_team"].Rating);
        Assert.Equal(2145, profile.Modes["rm_1v1_elo"].Rating);
        Assert.Null(profile.Modes["rm_1v1_elo"].RankLevel);
        Assert.Equal("legacy", profile.Modes["rm_1v1"].Notice);

        var primary = ModeCardBuilder.BuildPrimary(profile.Modes);
        Assert.Contains(primary, card => card.Title == "Ranked 1v1" && card.Rating == "1,729" && card.RankLevel == "Conqueror III" && card.History == "Earlier seasons 151–53.");
        Assert.Contains(primary, card => card.Title == "Ranked team" && card.RankLevel == "Unranked" && card.Rating == "—");
        Assert.DoesNotContain(primary, card => card.Title == "Quick match 2v2");
        Assert.DoesNotContain(primary, card => card.Title.Contains("legacy", StringComparison.OrdinalIgnoreCase));

        var inactive = ModeCardBuilder.BuildPrimary(new Dictionary<string, ModeStats>
        {
            ["rm_solo"] = new()
            {
                RankLevel = "unranked",
                GamesCount = 0,
                WinsCount = 0,
                LossesCount = 0,
                Season = 14,
                PreviousSeasons =
                [
                    new() { Season = 12, Rating = 958, Rank = 24759, RankLevel = "gold_3", WinsCount = 36, LossesCount = 38, GamesCount = 74, WinRate = 48.6 },
                    new() { Season = 6, Rating = 975, WinsCount = 110, LossesCount = 109, GamesCount = 219, WinRate = 50.2 },
                ],
            },
        });
        var ranked = Assert.Single(inactive);
        Assert.Equal("Ranked 1v1", ranked.Title);
        Assert.Equal("Gold III", ranked.RankLevel);
        Assert.Equal("958", ranked.Rating);
        Assert.Equal("36–38 W–L", ranked.Record);
        Assert.Equal("Season 12", ranked.Season);
        Assert.Equal("Season 14 is unranked. Career 146–147 across 2 earlier seasons.", ranked.History);

        var lifetime = ModeCardBuilder.BuildLifetime(profile.Modes);
        Assert.Single(lifetime);
        Assert.Equal("Ranked 1v1 ELO", lifetime[0].Title);
        Assert.Equal("Most played: Macedonian Dynasty", primary[0].TopCivilization);
    }

    [Fact]
    public async Task Games_fixture_reads_null_mmr_diff_and_string_per_page()
    {
        var handler = new StubHandler { Responder = (_, _) => JsonResponse(ReadFixture("games.json")) };
        using var http = new HttpClient(handler);
        var page = await new Aoe4WorldClient(http).GetPlayerGamesAsync(8139502, new PlayerGamesRequest { Limit = 1 });

        Assert.Equal(1, page.PerPage);
        var game = page.Games![0];
        Assert.Null(game.Teams![1][0].Player!.MmrDiff);
        var row = MatchRowBuilder.Build(game, 8139502);
        Assert.Equal("Win", row.Result);
        Assert.Equal("High View", row.Map);
        Assert.Equal("16:37", row.Duration);
        Assert.Equal("+26", row.RatingChange);
        Assert.Equal("French", row.Civilization);
        Assert.Contains("人机 (Abbasid Dynasty)", row.Opponents, StringComparison.Ordinal);
        Assert.Contains("Patch 11308", row.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Http_404_becomes_a_typed_error_without_throwing_json()
    {
        var handler = new StubHandler
        {
            Responder = (_, _) => new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"error\":\"missing\"}"),
            },
        };
        using var http = new HttpClient(handler);
        var ex = await Assert.ThrowsAsync<Aoe4WorldException>(() => new Aoe4WorldClient(http).GetPlayerAsync(42));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
        Assert.Contains("no record", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Quiet_refresh_does_not_call_the_network_again()
    {
        var clock = new ManualClock(new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero));
        var handler = new StubHandler { Responder = (_, _) => JsonResponse(ReadFixture("profile.json")) };
        using var http = new HttpClient(handler);
        var lookup = new Aoe4WorldLookup(new Aoe4WorldClient(http), new TtlMemoryCache(clock), clock);

        var first = await lookup.GetProfileAsync(8139502, forceRefresh: false, CancellationToken.None);
        clock.UtcNow = clock.UtcNow.AddSeconds(5);
        var second = await lookup.GetProfileAsync(8139502, forceRefresh: true, CancellationToken.None);
        clock.UtcNow = clock.UtcNow.AddSeconds(20);
        var third = await lookup.GetProfileAsync(8139502, forceRefresh: true, CancellationToken.None);

        Assert.False(first.FromCache);
        Assert.True(second.FromCache);
        Assert.True(second.QuietRefresh);
        Assert.False(third.FromCache);
        Assert.Equal(2, handler.Calls.Count);
    }

    [Fact]
    public void Cache_expires_and_profile_store_round_trips()
    {
        var clock = new ManualClock(new DateTimeOffset(2026, 11, 8, 0, 0, 0, TimeSpan.Zero));
        var cache = new TtlMemoryCache(clock);
        cache.Set("profile:1", new PlayerProfile { ProfileId = 1, Name = "A" }, TimeSpan.FromMinutes(5));
        Assert.True(cache.TryGet<PlayerProfile>("profile:1", out var hit, out _));
        Assert.Equal("A", hit!.Name);
        clock.UtcNow = clock.UtcNow.AddMinutes(5);
        Assert.False(cache.TryGet<PlayerProfile>("profile:1", out _, out _));

        var path = Path.Combine(Path.GetTempPath(), "aoe4stats-tests", Guid.NewGuid().ToString("N"), "selected-profile.json");
        var store = new JsonSelectedProfileStore(path);
        store.Save(new SelectedProfile
        {
            ProfileId = 8139502,
            Name = "Beasty",
            Country = "rs",
            SavedAt = clock.UtcNow,
        });
        var loaded = store.Load();
        Assert.NotNull(loaded);
        Assert.Equal(8139502, loaded.ProfileId);
        File.WriteAllText(path, "{not json");
        Assert.Null(store.Load());
        Assert.True(File.Exists(path + ".bad"));
    }

    [Fact]
    public void Display_formats_rank_country_and_rejects_foreign_links()
    {
        Assert.Equal("Conqueror III", AoE4Display.FormatRankLevel("conqueror_3"));
        Assert.Equal("Unranked", AoE4Display.FormatRankLevel(null));
        Assert.Equal("71.4%", AoE4Display.FormatWinRate(71.4, 5, 2));
        Assert.Equal("50.0%", AoE4Display.FormatWinRate(null, 1, 1));
        Assert.Equal("—", AoE4Display.FormatWinRate(null, 0, 0));
        Assert.Equal("Zhu Xi's Legacy", AoE4Display.FormatCivilization("zhu_xis_legacy"));
        Assert.Equal("Order of the Dragon", AoE4Display.FormatCivilization("order_of_the_dragon"));
        Assert.Equal("Serbia", AoE4Display.FormatCountry("rs"));
        Assert.True(AoE4Display.TryGetAoe4WorldUri("http://aoe4world.com/players/8139502", out var allowed));
        Assert.Equal("aoe4world.com", allowed!.Host);
        Assert.False(AoE4Display.TryGetAoe4WorldUri("https://example.com/players/1", out _));
        Assert.False(AoE4Display.TryGetAoe4WorldUri("javascript:alert(1)", out _));
    }

    [Fact]
    public async Task Search_view_model_shows_rows_and_selects_a_profile()
    {
        var clock = new ManualClock(DateTimeOffset.UnixEpoch);
        var handler = new StubHandler { Responder = (_, _) => JsonResponse(ReadFixture("search.json")) };
        using var http = new HttpClient(handler);
        var lookup = new Aoe4WorldLookup(new Aoe4WorldClient(http), new TtlMemoryCache(clock), clock);
        var path = Path.Combine(Path.GetTempPath(), "aoe4stats-tests", Guid.NewGuid().ToString("N"), "selected-profile.json");
        var session = new PlayerSession(new JsonSelectedProfileStore(path));
        var model = new SearchViewModel(lookup, session)
        {
            Query = "be",
        };

        Assert.False(model.SearchCommand.CanExecute(null));
        model.Query = "beasty";
        await model.SearchCommand.ExecuteAsync(null);

        Assert.True(model.HasResults);
        Assert.Equal("Beasty", model.Results[0].Name);
        Assert.Contains("Ranked 1v1", model.Results[0].Summary, StringComparison.Ordinal);
        model.SelectPlayerCommand.Execute(model.Results[0]);
        Assert.Equal(8139502, session.Current!.ProfileId);
        Assert.Equal("rs", session.Current.Country);
    }

    [Fact]
    public async Task Live_public_api_smoke_when_opted_in()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("AOE4STATS_LIVE_API"), "1", StringComparison.Ordinal))
        {
            return;
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        var client = new Aoe4WorldClient(http);
        var search = await client.SearchPlayersAsync("beasty", page: 1, exact: true);
        var player = search.Players?.FirstOrDefault(item => item.ProfileId > 0);
        Assert.NotNull(player);
        var profile = await client.GetPlayerAsync(player.ProfileId);
        Assert.Equal(player.ProfileId, profile.ProfileId);
        var games = await client.GetPlayerGamesAsync(player.ProfileId, new PlayerGamesRequest { Limit = 1 });
        Assert.NotNull(games.Games);
    }

    private static string ReadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };

    private sealed class ManualClock : IClock
    {
        public ManualClock(DateTimeOffset utcNow) => UtcNow = utcNow;

        public DateTimeOffset UtcNow { get; set; }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>? Responder { get; init; }

        public List<(string Uri, string UserAgent)> Calls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add((request.RequestUri?.AbsoluteUri ?? "", request.Headers.UserAgent.ToString()));
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Responder!(request, cancellationToken));
        }
    }
}
