using AoE4Stats.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AoE4Stats.Core.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly PlayerSession _session;

    public ShellViewModel(PlayerSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _session.Changed += (_, _) => Apply();
        Apply();
    }

    [ObservableProperty]
    private string _header = "No player selected";

    [ObservableProperty]
    private bool _hasPlayer;

    [RelayCommand]
    private void Forget() => _session.Clear();

    private void Apply()
    {
        var current = _session.Current;
        HasPlayer = current is not null;
        if (current is null)
        {
            Header = "No player selected";
            return;
        }

        Header = string.IsNullOrWhiteSpace(current.Country)
            ? current.Name
            : $"{current.Name} · {current.Country.Trim().ToUpperInvariant()}";
    }
}
