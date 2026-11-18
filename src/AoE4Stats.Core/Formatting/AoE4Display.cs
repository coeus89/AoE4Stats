using System.Globalization;

namespace AoE4Stats.Core.Formatting;

public static class AoE4Display
{
    private static readonly Dictionary<string, string> ModeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["rm_solo"] = "Ranked 1v1",
        ["rm_team"] = "Ranked team",
        ["rm_1v1"] = "Ranked 1v1",
        ["rm_2v2"] = "Ranked 2v2",
        ["rm_3v3"] = "Ranked 3v3",
        ["rm_4v4"] = "Ranked 4v4",
        ["qm_1v1"] = "Quick match 1v1",
        ["qm_2v2"] = "Quick match 2v2",
        ["qm_3v3"] = "Quick match 3v3",
        ["qm_4v4"] = "Quick match 4v4",
        ["rm_1v1_elo"] = "Ranked 1v1 ELO",
        ["rm_2v2_elo"] = "Ranked 2v2 ELO",
        ["rm_3v3_elo"] = "Ranked 3v3 ELO",
        ["rm_4v4_elo"] = "Ranked 4v4 ELO",
    };

    private static readonly Dictionary<string, string> CivilizationNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["zhu_xis_legacy"] = "Zhu Xi's Legacy",
        ["jeanne_darc"] = "Jeanne d'Arc",
        ["knights_templar"] = "Knights Templar",
        ["house_of_lancaster"] = "House of Lancaster",
        ["order_of_the_dragon"] = "Order of the Dragon",
        ["abbasid_dynasty"] = "Abbasid Dynasty",
        ["ayyubids"] = "Ayyubids",
        ["macedonian_dynasty"] = "Macedonian Dynasty",
        ["ottoman"] = "Ottomans",
        ["ottomans"] = "Ottomans",
        ["hre"] = "Holy Roman Empire",
        ["holy_roman_empire"] = "Holy Roman Empire",
        ["rus"] = "Rus",
        ["delhi_sultanate"] = "Delhi Sultanate",
        ["mongols"] = "Mongols",
        ["chinese"] = "Chinese",
        ["english"] = "English",
        ["french"] = "French",
        ["japanese"] = "Japanese",
        ["byzantines"] = "Byzantines",
        ["malians"] = "Malians",
        ["vikings"] = "Vikings",
        ["scots"] = "Scots",
    };

    public static string FormatMode(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "Unknown mode";
        }

        return ModeNames.TryGetValue(key, out var name) ? name : HumanizeToken(key);
    }

    public static string FormatCivilization(string? token) =>
        string.IsNullOrWhiteSpace(token) ? "—" : HumanizeToken(token);

    public static string HumanizeToken(string token)
    {
        if (CivilizationNames.TryGetValue(token, out var known))
        {
            return known;
        }

        var parts = token.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "—";
        }

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (i > 0 && part.Equals("of", StringComparison.OrdinalIgnoreCase)
                || i > 0 && part.Equals("the", StringComparison.OrdinalIgnoreCase)
                || i > 0 && part.Equals("and", StringComparison.OrdinalIgnoreCase))
            {
                parts[i] = part.ToLowerInvariant();
                continue;
            }

            parts[i] = part.Length == 1
                ? char.ToUpperInvariant(part[0]).ToString()
                : char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant();
        }

        return string.Join(' ', parts);
    }

    public static string FormatRankLevel(string? rankLevel)
    {
        if (string.IsNullOrWhiteSpace(rankLevel) || rankLevel.Equals("unranked", StringComparison.OrdinalIgnoreCase))
        {
            return "Unranked";
        }

        var parts = rankLevel.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2 && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var tier))
        {
            return $"{HumanizeToken(parts[0])} {ToRoman(tier)}";
        }

        return HumanizeToken(rankLevel);
    }

    public static string FormatWinRate(double? winRate, int? wins, int? losses)
    {
        if (winRate is double reported && !double.IsNaN(reported) && !double.IsInfinity(reported))
        {
            return reported.ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        var played = (wins ?? 0) + (losses ?? 0);
        if (played <= 0)
        {
            return "—";
        }

        var computed = 100d * (wins ?? 0) / played;
        return computed.ToString("0.0", CultureInfo.InvariantCulture) + "%";
    }

    public static string FormatRecord(int? wins, int? losses)
    {
        if (wins is null && losses is null)
        {
            return "—";
        }

        return string.Create(CultureInfo.InvariantCulture, $"{wins ?? 0:N0}–{losses ?? 0:N0}");
    }

    public static string FormatRank(int? rank) =>
        rank is int value && value > 0 ? "#" + value.ToString("N0", CultureInfo.InvariantCulture) : "—";

    public static string FormatRating(int? rating) =>
        rating is int value ? value.ToString("N0", CultureInfo.InvariantCulture) : "—";

    public static string FormatRatingDiff(int? diff)
    {
        if (diff is not int value)
        {
            return "—";
        }

        return value > 0
            ? "+" + value.ToString(CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
    }

    public static string FormatStreak(int? streak)
    {
        if (streak is not int value)
        {
            return "—";
        }

        if (value > 0)
        {
            return "+" + value.ToString(CultureInfo.InvariantCulture);
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static string FormatTimestamp(DateTimeOffset? timestamp)
    {
        if (timestamp is not DateTimeOffset value)
        {
            return "—";
        }

        return value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC";
    }

    public static string FormatDuration(int? seconds)
    {
        if (seconds is not int value || value < 0)
        {
            return "—";
        }

        var time = TimeSpan.FromSeconds(value);
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes}:{time.Seconds:00}";
    }

    public static string FormatCountry(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return "—";
        }

        var code = countryCode.Trim().ToUpperInvariant();
        try
        {
            return new RegionInfo(code).EnglishName;
        }
        catch (ArgumentException)
        {
            return code;
        }
    }

    public static bool TryGetAoe4WorldUri(string? siteUrl, out Uri? uri)
    {
        uri = null;
        if (!Uri.TryCreate(siteUrl, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        if (parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp)
        {
            return false;
        }

        if (!parsed.Host.Equals("aoe4world.com", StringComparison.OrdinalIgnoreCase)
            && !parsed.Host.EndsWith(".aoe4world.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    public static string FormatFreshness(DateTimeOffset fetchedAt, bool fromCache, bool quietRefresh)
    {
        var stamp = fetchedAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";
        if (quietRefresh)
        {
            return $"Still using the result from {stamp}. Refresh is ignored for a few seconds so this app does not hammer AoE4 World.";
        }

        return fromCache ? $"Cached from {stamp}." : $"Fetched {stamp}.";
    }

    private static string ToRoman(int value) => value switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        5 => "V",
        _ => value.ToString(CultureInfo.InvariantCulture),
    };
}
