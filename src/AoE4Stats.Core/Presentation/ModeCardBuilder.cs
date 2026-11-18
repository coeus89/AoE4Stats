using AoE4Stats.Core.Api;
using AoE4Stats.Core.Formatting;

namespace AoE4Stats.Core.Presentation;

public static class ModeCardBuilder
{
    private static readonly string[] PrimaryOrder =
    [
        "rm_solo",
        "rm_team",
        "qm_1v1",
        "qm_2v2",
        "qm_3v3",
        "qm_4v4",
    ];

    private static readonly string[] LifetimeOrder =
    [
        "rm_1v1_elo",
        "rm_2v2_elo",
        "rm_3v3_elo",
        "rm_4v4_elo",
    ];

    public static IReadOnlyList<ModeCard> BuildPrimary(IReadOnlyDictionary<string, ModeStats>? modes)
    {
        if (modes is null || modes.Count == 0)
        {
            return [];
        }

        var cards = new List<ModeCard>();
        var shown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasSolo = modes.ContainsKey("rm_solo");
        foreach (var key in PrimaryOrder)
        {
            if (!modes.TryGetValue(key, out var stats))
            {
                continue;
            }

            var games = stats.GamesCount ?? 0;
            var headline = key is "rm_solo" or "rm_team";
            if (!headline && games <= 0 && stats.Rating is null)
            {
                continue;
            }

            cards.Add(Create(key, stats));
            shown.Add(key);
        }

        if (!hasSolo && modes.TryGetValue("rm_1v1", out var legacy))
        {
            cards.Insert(0, Create("rm_1v1", legacy));
            shown.Add("rm_1v1");
        }

        foreach (var pair in modes.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (shown.Contains(pair.Key) || pair.Key.EndsWith("_elo", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (hasSolo && pair.Key.Equals("rm_1v1", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var games = pair.Value.GamesCount ?? 0;
            if (games <= 0 && pair.Value.Rating is null)
            {
                continue;
            }

            cards.Add(Create(pair.Key, pair.Value));
        }

        return cards;
    }

    public static IReadOnlyList<ModeCard> BuildLifetime(IReadOnlyDictionary<string, ModeStats>? modes)
    {
        if (modes is null || modes.Count == 0)
        {
            return [];
        }

        var cards = new List<ModeCard>();
        var shown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in LifetimeOrder)
        {
            if (!modes.TryGetValue(key, out var stats) || stats.Rating is null)
            {
                continue;
            }

            cards.Add(Create(key, stats));
            shown.Add(key);
        }

        foreach (var pair in modes.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (shown.Contains(pair.Key) || !pair.Key.EndsWith("_elo", StringComparison.OrdinalIgnoreCase) || pair.Value.Rating is null)
            {
                continue;
            }

            cards.Add(Create(pair.Key, pair.Value));
        }

        return cards;
    }

    private static ModeCard Create(string key, ModeStats stats)
    {
        var top = stats.Civilizations?
            .Where(civ => !string.IsNullOrWhiteSpace(civ.Civilization))
            .OrderByDescending(civ => civ.GamesCount ?? 0)
            .FirstOrDefault();

        // Current season is often 0–0 while previous_seasons holds the ranked record.
        var shown = HasLadderSnapshot(stats)
            ? stats
            : LatestPlayedSeason(stats.PreviousSeasons) ?? stats;

        return new ModeCard
        {
            Title = AoE4Display.FormatMode(key),
            RankLevel = AoE4Display.FormatRankLevel(shown.RankLevel),
            Rating = AoE4Display.FormatRating(shown.Rating),
            Rank = AoE4Display.FormatRank(shown.Rank),
            Record = AoE4Display.FormatRecord(shown.WinsCount, shown.LossesCount) + " W–L",
            WinRate = "Win rate " + AoE4Display.FormatWinRate(shown.WinRate, shown.WinsCount, shown.LossesCount),
            Streak = "Streak " + AoE4Display.FormatStreak(shown.Streak),
            LastGame = AoE4Display.FormatTimestamp(shown.LastGameAt),
            Season = shown.Season is int season ? $"Season {season}" : "",
            History = FormatHistory(stats),
            TopCivilization = top is null
                ? ""
                : $"Most played: {AoE4Display.FormatCivilization(top.Civilization)}",
        };
    }

    private static bool HasLadderSnapshot(ModeStats stats) =>
        stats.Rating is not null
        || (stats.GamesCount ?? 0) > 0
        || (stats.WinsCount ?? 0) + (stats.LossesCount ?? 0) > 0;

    private static ModeStats? LatestPlayedSeason(IEnumerable<ModeStats>? seasons) =>
        seasons?
            .Where(HasLadderSnapshot)
            .OrderByDescending(season => season.Season ?? int.MinValue)
            .FirstOrDefault();

    private static string FormatHistory(ModeStats current)
    {
        var earlier = SumSeasons(current.PreviousSeasons);
        if (earlier.Seasons == 0)
        {
            return "";
        }

        var record = AoE4Display.FormatRecord(earlier.Wins, earlier.Losses);
        if (HasLadderSnapshot(current))
        {
            return $"Earlier seasons {record}.";
        }

        var seasonLabel = current.Season is int season
            ? $"Season {season} is unranked. "
            : "This season is unranked. ";
        var noun = earlier.Seasons == 1 ? "season" : "seasons";
        return $"{seasonLabel}Career {record} across {earlier.Seasons} earlier {noun}.";
    }

    private static (int Wins, int Losses, int Seasons) SumSeasons(IEnumerable<ModeStats>? seasons)
    {
        if (seasons is null)
        {
            return (0, 0, 0);
        }

        var wins = 0;
        var losses = 0;
        var count = 0;
        foreach (var season in seasons)
        {
            if (!HasLadderSnapshot(season))
            {
                continue;
            }

            wins += season.WinsCount ?? 0;
            losses += season.LossesCount ?? 0;
            count++;
        }

        return (wins, losses, count);
    }
}
