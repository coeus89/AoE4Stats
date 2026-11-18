using System.Text.Json.Serialization;

namespace AoE4Stats.Core.Storage;

/// <summary>Local snapshot of the player the app should reopen. Not a copy of the full profile.</summary>
public sealed class SelectedProfile
{
    [JsonPropertyName("profileId")]
    public long ProfileId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("savedAt")]
    public DateTimeOffset SavedAt { get; set; }
}
