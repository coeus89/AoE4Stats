using System.Globalization;
using System.Text;
using AoE4Stats.Core.Api;
using AoE4Stats.Core.Formatting;

namespace AoE4Stats.Core.Presentation;

public static class MatchRowBuilder
{
    public static IReadOnlyList<MatchRow> Build(IEnumerable<PlayerGame>? games, long profileId)
    {
        if (games is null)
        {
            return [];
        }

        return games.Select(game => Build(game, profileId)).ToArray();
    }

    public static MatchRow Build(PlayerGame game, long profileId)
    {
        ArgumentNullException.ThrowIfNull(game);
        var self = FindSelf(game, profileId);
        var detail = new StringBuilder();
        if (game.Patch is int patch)
        {
            detail.Append("Patch ").Append(patch.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(game.Server))
        {
            if (detail.Length > 0)
            {
                detail.Append(" · ");
            }

            detail.Append(game.Server);
        }

        var duration = AoE4Display.FormatDuration(game.Duration);
        if (duration != "—")
        {
            if (detail.Length > 0)
            {
                detail.Append(" · ");
            }

            detail.Append(duration);
        }

        return new MatchRow
        {
            When = AoE4Display.FormatTimestamp(game.StartedAt),
            Mode = AoE4Display.FormatMode(game.Kind ?? game.Leaderboard),
            Map = string.IsNullOrWhiteSpace(game.Map) ? "Unknown map" : game.Map.Trim(),
            Result = FormatResult(self?.Result, game.Ongoing),
            Civilization = AoE4Display.FormatCivilization(self?.Civilization),
            RatingChange = AoE4Display.FormatRatingDiff(self?.RatingDiff),
            Opponents = DescribeOthers(game, profileId),
            Duration = duration,
            Detail = detail.Length == 0 ? "—" : detail.ToString(),
        };
    }

    private static GamePlayer? FindSelf(PlayerGame game, long profileId)
    {
        if (game.Teams is null)
        {
            return null;
        }

        foreach (var team in game.Teams)
        {
            foreach (var slot in team)
            {
                if (slot.Player?.ProfileId == profileId)
                {
                    return slot.Player;
                }
            }
        }

        return null;
    }

    private static string DescribeOthers(PlayerGame game, long profileId)
    {
        if (game.Teams is null || game.Teams.Count == 0)
        {
            return "—";
        }

        var allies = new List<string>();
        var opponents = new List<string>();
        var selfTeam = -1;
        for (var teamIndex = 0; teamIndex < game.Teams.Count; teamIndex++)
        {
            if (game.Teams[teamIndex].Any(slot => slot.Player?.ProfileId == profileId))
            {
                selfTeam = teamIndex;
                break;
            }
        }

        for (var teamIndex = 0; teamIndex < game.Teams.Count; teamIndex++)
        {
            foreach (var slot in game.Teams[teamIndex])
            {
                var player = slot.Player;
                if (player is null || player.ProfileId == profileId)
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(player.Name) ? $"Profile {player.ProfileId}" : player.Name.Trim();
                if (!string.IsNullOrWhiteSpace(player.Civilization))
                {
                    label += $" ({AoE4Display.FormatCivilization(player.Civilization)})";
                }

                if (selfTeam >= 0 && teamIndex == selfTeam)
                {
                    allies.Add(label);
                }
                else
                {
                    opponents.Add(label);
                }
            }
        }

        if (allies.Count == 0 && opponents.Count == 0)
        {
            return "—";
        }

        if (allies.Count == 0)
        {
            return "vs " + string.Join(", ", opponents);
        }

        if (opponents.Count == 0)
        {
            return "with " + string.Join(", ", allies);
        }

        return "with " + string.Join(", ", allies) + " · vs " + string.Join(", ", opponents);
    }

    private static string FormatResult(string? result, bool? ongoing)
    {
        if (string.Equals(result, "win", StringComparison.OrdinalIgnoreCase)
            || string.Equals(result, "victory", StringComparison.OrdinalIgnoreCase))
        {
            return "Win";
        }

        if (string.Equals(result, "loss", StringComparison.OrdinalIgnoreCase)
            || string.Equals(result, "defeat", StringComparison.OrdinalIgnoreCase))
        {
            return "Loss";
        }

        if (ongoing == true)
        {
            return "In progress";
        }

        return string.IsNullOrWhiteSpace(result) ? "Unknown" : AoE4Display.HumanizeToken(result);
    }
}
