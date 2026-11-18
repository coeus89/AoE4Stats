using AoE4Stats.Core.Api;
using AoE4Stats.Core.Storage;

namespace AoE4Stats.Core.Services;

public sealed class PlayerSession
{
    private readonly ISelectedProfileStore _store;

    public PlayerSession(ISelectedProfileStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        Current = store.Load();
    }

    public SelectedProfile? Current { get; private set; }

    public event EventHandler? Changed;

    public void Select(PlayerSummary player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (player.ProfileId <= 0)
        {
            throw new ArgumentException("Search result is missing a profile id.", nameof(player));
        }

        var profile = new SelectedProfile
        {
            ProfileId = player.ProfileId,
            Name = string.IsNullOrWhiteSpace(player.Name) ? $"Profile {player.ProfileId}" : player.Name.Trim(),
            Country = player.Country,
            SavedAt = DateTimeOffset.UtcNow,
        };
        _store.Save(profile);
        Current = profile;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void RememberDetails(PlayerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (Current is null || Current.ProfileId != profile.ProfileId)
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(profile.Name) ? Current.Name : profile.Name.Trim();
        if (name == Current.Name && string.Equals(profile.Country, Current.Country, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var updated = new SelectedProfile
        {
            ProfileId = Current.ProfileId,
            Name = name,
            Country = profile.Country,
            SavedAt = DateTimeOffset.UtcNow,
        };
        _store.Save(updated);
        Current = updated;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _store.Clear();
        Current = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
