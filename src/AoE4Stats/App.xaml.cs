using System.Net;
using System.Net.Http.Headers;
using AoE4Stats.Core.Api;
using AoE4Stats.Core.Caching;
using AoE4Stats.Core.Services;
using AoE4Stats.Core.Storage;
using AoE4Stats.Core.ViewModels;
using Microsoft.UI.Xaml;

namespace AoE4Stats;

public partial class App : Application
{
    public App()
    {
        Services = new AppServices();
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    public static new App Current => (App)Application.Current;

    public AppServices Services { get; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new MainWindow();
        window.Activate();
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AoE4Stats");
            Directory.CreateDirectory(directory);
            File.AppendAllText(
                Path.Combine(directory, "error.log"),
                $"{DateTimeOffset.Now:u}{Environment.NewLine}{e.Exception}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // The crash dialog is still the user-facing signal.
        }
    }
}

public sealed class AppServices
{
    public AppServices()
    {
        Http = CreateHttp();
        Client = new Aoe4WorldClient(Http);
        var clock = new SystemClock();
        Cache = new TtlMemoryCache(clock);
        Lookup = new Aoe4WorldLookup(Client, Cache, clock);
        Profiles = new JsonSelectedProfileStore(JsonSelectedProfileStore.DefaultPath);
        Session = new PlayerSession(Profiles);
        Shell = new ShellViewModel(Session);
        Search = new SearchViewModel(Lookup, Session);
        Dashboard = new DashboardViewModel(Lookup, Session);
        Matches = new MatchesViewModel(Lookup, Session);
    }

    public HttpClient Http { get; }

    public Aoe4WorldClient Client { get; }

    public TtlMemoryCache Cache { get; }

    public Aoe4WorldLookup Lookup { get; }

    public JsonSelectedProfileStore Profiles { get; }

    public PlayerSession Session { get; }

    public ShellViewModel Shell { get; }

    public SearchViewModel Search { get; }

    public DashboardViewModel Dashboard { get; }

    public MatchesViewModel Matches { get; }

    private static HttpClient CreateHttp()
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        };
        var http = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(20),
        };
        http.DefaultRequestHeaders.UserAgent.ParseAdd(Aoe4WorldClient.ProductUserAgent);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return http;
    }
}
