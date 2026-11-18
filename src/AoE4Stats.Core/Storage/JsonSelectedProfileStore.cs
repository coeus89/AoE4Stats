using System.Text.Json;
using AoE4Stats.Core.Api;

namespace AoE4Stats.Core.Storage;

public interface ISelectedProfileStore
{
    SelectedProfile? Load();

    void Save(SelectedProfile profile);

    void Clear();
}

/// <summary>
/// Writes one JSON file. Packaged runs see the package's virtualized LocalAppData folder.
/// </summary>
public sealed class JsonSelectedProfileStore : ISelectedProfileStore
{
    private readonly string _path;

    public JsonSelectedProfileStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AoE4Stats",
        "selected-profile.json");

    public SelectedProfile? Load()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_path);
            var profile = JsonSerializer.Deserialize(json, Aoe4JsonContext.Default.SelectedProfile);
            if (profile is null || profile.ProfileId <= 0 || string.IsNullOrWhiteSpace(profile.Name))
            {
                return null;
            }

            return profile;
        }
        catch (JsonException)
        {
            Quarantine();
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public void Save(SelectedProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.ProfileId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(profile));
        }

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            throw new ArgumentException("A selected profile needs a display name.", nameof(profile));
        }

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(profile, Aoe4JsonContext.Default.SelectedProfile);
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, json);
        File.Move(temporary, _path, overwrite: true);
    }

    public void Clear()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    private void Quarantine()
    {
        try
        {
            var bad = _path + ".bad";
            File.Move(_path, bad, overwrite: true);
        }
        catch (IOException)
        {
            // Leave the unreadable file in place. The next launch will try again.
        }
    }
}
