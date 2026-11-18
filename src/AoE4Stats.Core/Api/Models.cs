using System.Text.Json.Serialization;

namespace AoE4Stats.Core.Api;

/// <summary>
/// Response from <c>GET /api/v0/players/search</c>.
/// Field names match the public AoE4 World payload. Missing values stay null.
/// </summary>
public sealed class PlayerSearchPage
{
    [JsonPropertyName("total_count")]
    public int? TotalCount { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("per_page")]
    public int? PerPage { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }

    [JsonPropertyName("offset")]
    public int? Offset { get; set; }

    [JsonPropertyName("next_page")]
    public int? NextPage { get; set; }

    [JsonPropertyName("players")]
    public List<PlayerSummary>? Players { get; set; }
}

/// <summary>One row from player search. Ratings live under <see cref="Leaderboards"/>.</summary>
public sealed class PlayerSummary
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("profile_id")]
    public long ProfileId { get; set; }

    [JsonPropertyName("steam_id")]
    public string? SteamId { get; set; }

    [JsonPropertyName("avatars")]
    public AvatarSet? Avatars { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("social")]
    public SocialLinks? Social { get; set; }

    [JsonPropertyName("last_game_at")]
    public DateTimeOffset? LastGameAt { get; set; }

    [JsonPropertyName("leaderboards")]
    public Dictionary<string, ModeStats>? Leaderboards { get; set; }
}

/// <summary>
/// Response from <c>GET /api/v0/players/{id}</c>.
/// The profile payload uses <see cref="Modes"/> where search uses leaderboards.
/// </summary>
public sealed class PlayerProfile
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("profile_id")]
    public long ProfileId { get; set; }

    [JsonPropertyName("steam_id")]
    public string? SteamId { get; set; }

    [JsonPropertyName("site_url")]
    public string? SiteUrl { get; set; }

    [JsonPropertyName("avatars")]
    public AvatarSet? Avatars { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("social")]
    public SocialLinks? Social { get; set; }

    [JsonPropertyName("modes")]
    public Dictionary<string, ModeStats>? Modes { get; set; }
}

public sealed class AvatarSet
{
    [JsonPropertyName("small")]
    public string? Small { get; set; }

    [JsonPropertyName("medium")]
    public string? Medium { get; set; }

    [JsonPropertyName("full")]
    public string? Full { get; set; }
}

public sealed class SocialLinks
{
    [JsonPropertyName("twitch")]
    public string? Twitch { get; set; }

    [JsonPropertyName("twitter")]
    public string? Twitter { get; set; }

    [JsonPropertyName("youtube")]
    public string? Youtube { get; set; }

    [JsonPropertyName("instagram")]
    public string? Instagram { get; set; }

    [JsonPropertyName("liquipedia")]
    public string? Liquipedia { get; set; }
}

/// <summary>
/// Seasonal ladder or lifetime ELO block. Unranked entries omit rating, rank, and win rate.
/// <c>rating_history</c> is intentionally not mapped; it is large and this starter does not chart it.
/// </summary>
public sealed class ModeStats
{
    [JsonPropertyName("_notice_")]
    public string? Notice { get; set; }

    [JsonPropertyName("rating")]
    public int? Rating { get; set; }

    [JsonPropertyName("max_rating")]
    public int? MaxRating { get; set; }

    [JsonPropertyName("max_rating_7d")]
    public int? MaxRating7d { get; set; }

    [JsonPropertyName("max_rating_1m")]
    public int? MaxRating1m { get; set; }

    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    [JsonPropertyName("rank_level")]
    public string? RankLevel { get; set; }

    [JsonPropertyName("streak")]
    public int? Streak { get; set; }

    [JsonPropertyName("games_count")]
    public int? GamesCount { get; set; }

    [JsonPropertyName("wins_count")]
    public int? WinsCount { get; set; }

    [JsonPropertyName("losses_count")]
    public int? LossesCount { get; set; }

    [JsonPropertyName("disputes_count")]
    public int? DisputesCount { get; set; }

    [JsonPropertyName("drops_count")]
    public int? DropsCount { get; set; }

    [JsonPropertyName("last_game_at")]
    public DateTimeOffset? LastGameAt { get; set; }

    [JsonPropertyName("win_rate")]
    public double? WinRate { get; set; }

    [JsonPropertyName("season")]
    public int? Season { get; set; }

    [JsonPropertyName("civilizations")]
    public List<CivilizationStat>? Civilizations { get; set; }

    [JsonPropertyName("previous_seasons")]
    public List<ModeStats>? PreviousSeasons { get; set; }
}

public sealed class CivilizationStat
{
    [JsonPropertyName("civilization")]
    public string? Civilization { get; set; }

    [JsonPropertyName("win_rate")]
    public double? WinRate { get; set; }

    [JsonPropertyName("pick_rate")]
    public double? PickRate { get; set; }

    [JsonPropertyName("games_count")]
    public int? GamesCount { get; set; }
}

/// <summary>Response from <c>GET /api/v0/players/{id}/games</c>.</summary>
public sealed class PlayerGamesPage
{
    [JsonPropertyName("total_count")]
    public int? TotalCount { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("per_page")]
    public int? PerPage { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }

    [JsonPropertyName("offset")]
    public int? Offset { get; set; }

    [JsonPropertyName("next_page")]
    public int? NextPage { get; set; }

    [JsonPropertyName("games")]
    public List<PlayerGame>? Games { get; set; }
}

public sealed class PlayerGame
{
    [JsonPropertyName("game_id")]
    public long GameId { get; set; }

    [JsonPropertyName("started_at")]
    public DateTimeOffset? StartedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }

    [JsonPropertyName("duration")]
    public int? Duration { get; set; }

    [JsonPropertyName("map")]
    public string? Map { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("leaderboard")]
    public string? Leaderboard { get; set; }

    [JsonPropertyName("season")]
    public int? Season { get; set; }

    [JsonPropertyName("server")]
    public string? Server { get; set; }

    [JsonPropertyName("patch")]
    public int? Patch { get; set; }

    [JsonPropertyName("ongoing")]
    public bool? Ongoing { get; set; }

    [JsonPropertyName("just_finished")]
    public bool? JustFinished { get; set; }

    [JsonPropertyName("teams")]
    public List<List<GameTeamSlot>>? Teams { get; set; }
}

public sealed class GameTeamSlot
{
    [JsonPropertyName("player")]
    public GamePlayer? Player { get; set; }
}

public sealed class GamePlayer
{
    [JsonPropertyName("profile_id")]
    public long ProfileId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("civilization")]
    public string? Civilization { get; set; }

    [JsonPropertyName("civilization_randomized")]
    public bool? CivilizationRandomized { get; set; }

    [JsonPropertyName("rating")]
    public int? Rating { get; set; }

    [JsonPropertyName("rating_diff")]
    public int? RatingDiff { get; set; }

    [JsonPropertyName("mmr")]
    public int? Mmr { get; set; }

    [JsonPropertyName("mmr_diff")]
    public int? MmrDiff { get; set; }

    [JsonPropertyName("input_type")]
    public string? InputType { get; set; }
}

public sealed class PlayerGamesRequest
{
    public int Page { get; init; } = 1;

    public int Limit { get; init; } = Aoe4WorldClient.DefaultGamesLimit;

    /// <summary>
    /// Optional ladder filter. Allowed values are the player-games leaderboards documented by AoE4 World.
    /// Null requests the mixed recent list.
    /// </summary>
    public string? Leaderboard { get; init; }

    /// <summary>Optional lower bound. Sent as an ISO-8601 UTC timestamp when set.</summary>
    public DateTimeOffset? Since { get; init; }
}
