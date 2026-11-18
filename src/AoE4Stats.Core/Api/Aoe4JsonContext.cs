using System.Text.Json.Serialization;
using AoE4Stats.Core.Storage;

namespace AoE4Stats.Core.Api;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true)]
[JsonSerializable(typeof(PlayerSearchPage))]
[JsonSerializable(typeof(PlayerProfile))]
[JsonSerializable(typeof(PlayerGamesPage))]
[JsonSerializable(typeof(SelectedProfile))]
internal partial class Aoe4JsonContext : JsonSerializerContext;
