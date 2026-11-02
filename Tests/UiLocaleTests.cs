using System.Globalization;
using CursorPace.Models;
using CursorPace.Services;
using CursorPace.ViewModels;

namespace CursorPace.Tests;

public sealed class UiLocaleTests : IDisposable
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "cursorpace-ui-locale-" + Guid.NewGuid().ToString("N"));

    public UiLocaleTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void TargetLocale_TranslatesCaptionAndValidation_AndKeepsStoredTheme()
    {
        var store = new JsonPlanStore(_directory);
        store.Save(new AppSettings { ThemeMode = UiThemeMode.Light });

        File.WriteAllText(Path.Combine(_directory, "es.json"), """
            {
              "Could not write the backup file.": "No se pudo escribir el archivo de copia de seguridad.",
              "Light": "Claro",
              "Saved to {0}": "Guardado en {0}",
              "This sentence is not in the catalog.": ""
            }
            """);
        I18n.Configure(new I18nOptions
        {
            SourceLocale = "en",
            FlatDirectory = _directory,
            Strict = false
        });
        var spanish = CultureInfo.GetCultureInfo("es");
        CultureInfo.CurrentCulture = spanish;
        CultureInfo.CurrentUICulture = spanish;
        I18n.SetLocale("es");

        Assert.Equal("Claro", new ThemeModeChoice(UiThemeMode.Light).ToString());
        Assert.Equal(
            "No se pudo escribir el archivo de copia de seguridad.",
            UiText.Translate("Could not write the backup file."));
        Assert.Equal("Guardado en /tmp/notes.zip", I18n.T("Saved to {0}", "/tmp/notes.zip"));
        Assert.Equal("This sentence is not in the catalog.", I18n.T("This sentence is not in the catalog."));

        var vm = new MainViewModel(
            new FixedClock(new DateTime(2026, 8, 18, 12, 0, 0)),
            new CycleCalculator(),
            store,
            new NoOpStartup(),
            new IdleSync(),
            new DataBackupService(store, new EmptySampleStore()),
            new IdleRemoteSync(),
            new InlineDispatcher());
        Assert.False(vm.TryWriteBackup(new ThrowOnWriteStream(), out var error));
        Assert.Equal("No se pudo escribir el archivo de copia de seguridad.", error);

        vm.UiLocale = "es";
        Assert.Equal(UiThemeMode.Light, vm.ThemeMode);
        Assert.Equal("es", vm.UiLocale);
        var json = JsonPlanStore.Serialize(store.Load());
        Assert.Contains("\"themeMode\": \"Light\"", json, StringComparison.Ordinal);
        Assert.Contains("\"uiLocale\": \"es\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Claro", json, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
        I18n.Configure(new I18nOptions
        {
            SourceLocale = "en",
            FlatDirectory = null,
            OpenLocale = null,
            Strict = false
        });
        I18n.SetLocale(I18n.SourceLocale);
        try
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private sealed class ThrowOnWriteStream : MemoryStream
    {
        public override void Write(byte[] buffer, int offset, int count) => throw new IOException("");

        public override void Write(ReadOnlySpan<byte> buffer) => throw new IOException("");

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            throw new IOException("");
    }

    private sealed class FixedClock(DateTime now) : IClock
    {
        public DateTime Now { get; } = now;
        public DateTime Today => Now.Date;
    }

    private sealed class NoOpStartup : IStartupRegistration
    {
        public bool IsRegistered => false;
        public void Register(bool startInTray) { }
        public void Unregister() { }
    }

    private sealed class EmptySampleStore : IUsageSampleStore
    {
        public UsageSampleDocument Load() => new();
        public void Save(UsageSampleDocument document) { }
    }

    private sealed class IdleSync : IUsageSyncService
    {
        public SyncStatus Status => SyncStatus.SignedOut;
        public bool IsSignedIn => false;
        public string StatusText => "Not signed in";
        public DateTimeOffset? LastSuccessUtc => null;
        public IReadOnlyList<UsageSample> Samples => [];
        public DateTimeOffset? SamplesCycleStartUtc => null;
        public event EventHandler? StateChanged { add { } remove { } }
        public event EventHandler<UsageSnapshot>? SnapshotReceived { add { } remove { } }
        public event EventHandler<UsageSnapshot>? SampleAppended { add { } remove { } }
        public Task StartAsync(bool autoSyncEnabled, int intervalHours) => Task.CompletedTask;
        public Task RefreshNowAsync(bool allowInteractiveLogin) => Task.CompletedTask;
        public Task SignInAsync() => Task.CompletedTask;
        public Task DisconnectAsync() => Task.CompletedTask;
        public void SetIntervalHours(int hours) { }
        public void SetAutoSyncEnabled(bool enabled) { }
        public void ReloadPersistedUsage(DateTimeOffset? lastSuccessUtc) { }
        public int MergeRemoteSamples(IReadOnlyList<UsageSample> remoteSamples, DateTimeOffset? remoteCycleStartUtc) => 0;
        public void Dispose() { }
    }

    private sealed class IdleRemoteSync : IRemoteSyncService
    {
        public Task<RemoteSyncResult> SyncAsync(RemoteSyncLocalState local, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RemoteSyncResult(false, null, null, 0, 0));
    }

    private sealed class InlineDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;
        public void Post(Action action) => action();
        public IUiTimer CreateTimer() => new IdleTimer();
    }

    private sealed class IdleTimer : IUiTimer
    {
        public TimeSpan Interval { get; set; }
        public bool IsRepeating { get; set; }
        public event EventHandler? Tick { add { } remove { } }
        public void Start() { }
        public void Stop() { }
    }
}
