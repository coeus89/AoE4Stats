namespace AoE4Stats.Core.Presentation;

public sealed class PlayerSearchItem
{
    public required string Name { get; init; }

    public long ProfileId { get; init; }

    public required string IdText { get; init; }

    public required string Country { get; init; }

    public string? CountryCode { get; init; }

    public required string Summary { get; init; }

    public required string LastGame { get; init; }

    public Uri? AvatarUri { get; init; }
}

public sealed class ModeCard
{
    public required string Title { get; init; }

    public required string RankLevel { get; init; }

    public required string Rating { get; init; }

    public required string Rank { get; init; }

    public required string Record { get; init; }

    public required string WinRate { get; init; }

    public required string Streak { get; init; }

    public required string LastGame { get; init; }

    public required string Season { get; init; }

    public required string History { get; init; }

    public required string TopCivilization { get; init; }
}

public sealed class MatchRow
{
    public required string When { get; init; }

    public required string Mode { get; init; }

    public required string Map { get; init; }

    public required string Result { get; init; }

    public required string Civilization { get; init; }

    public required string RatingChange { get; init; }

    public required string Opponents { get; init; }

    public required string Duration { get; init; }

    public required string Detail { get; init; }
}

public sealed class ModeFilter
{
    public required string Label { get; init; }

    public string? Leaderboard { get; init; }
}
