namespace CursorPace.Models;

public sealed class AppSettings
{
    public int Version { get; set; } = 2;
    public QuotaCycle? ActiveCycle { get; set; }
    public List<QuotaCycle> CycleHistory { get; set; } = new();
    public bool RunAtStartup { get; set; }
    public bool StartInNotificationTray { get; set; } = true;
    public UiThemeMode ThemeMode { get; set; } = UiThemeMode.System;
    public bool AutoSyncEnabled { get; set; } = true;
    public int SyncIntervalHours { get; set; } = 1;
    public bool ShowChartView { get; set; }
    public bool CursorAccountConnected { get; set; }
    public DateTimeOffset? LastUsageSyncUtc { get; set; }
    public int? WindowX { get; set; }
    public int? WindowY { get; set; }
    public int? WindowWidth { get; set; }
    public int? WindowHeight { get; set; }
    public bool WindowMaximized { get; set; }
    public bool RemoteSyncEnabled { get; set; }
    public string? RemoteSyncUrl { get; set; }
    public string? RemoteSyncApiKey { get; set; }
    public string? RemoteSyncMachineName { get; set; }
    public DateTimeOffset? LastRemoteSyncUtc { get; set; }
}
