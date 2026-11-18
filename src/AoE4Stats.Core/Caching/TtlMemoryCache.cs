namespace AoE4Stats.Core.Caching;

/// <summary>Small in-memory TTL cache. Process-local, not a disk crawl of the API.</summary>
public sealed class TtlMemoryCache
{
    private readonly IClock _clock;
    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public TtlMemoryCache(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    public bool TryGet<T>(string key, out T? value, out DateTimeOffset storedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        lock (_gate)
        {
            if (!_entries.TryGetValue(key, out var entry) || entry.ExpiresAt <= _clock.UtcNow || entry.Value is not T typed)
            {
                if (entry is not null && entry.ExpiresAt <= _clock.UtcNow)
                {
                    _entries.Remove(key);
                }

                value = default;
                storedAt = default;
                return false;
            }

            value = typed;
            storedAt = entry.StoredAt;
            return true;
        }
    }

    public void Set<T>(string key, T value, TimeSpan ttl)
        where T : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        if (ttl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl));
        }

        var now = _clock.UtcNow;
        lock (_gate)
        {
            _entries[key] = new Entry
            {
                Value = value,
                StoredAt = now,
                ExpiresAt = now.Add(ttl),
            };
        }
    }

    public void Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        lock (_gate)
        {
            _entries.Remove(key);
        }
    }

    private sealed class Entry
    {
        public required object Value { get; init; }

        public required DateTimeOffset StoredAt { get; init; }

        public required DateTimeOffset ExpiresAt { get; init; }
    }
}
