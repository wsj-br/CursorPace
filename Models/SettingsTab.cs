namespace CursorPace.Models;

public enum SettingsTab
{
    Account,
    SyncServer,
    Startup,
    Export,
    About
}

public static class SettingsTabIds
{
    public static SettingsTab Clamp(SettingsTab tab) => tab switch
    {
        SettingsTab.Account => SettingsTab.Account,
        SettingsTab.SyncServer => SettingsTab.SyncServer,
        SettingsTab.Startup => SettingsTab.Startup,
        SettingsTab.Export => SettingsTab.Export,
        SettingsTab.About => SettingsTab.About,
        _ => SettingsTab.Startup
    };

    public static SettingsTab Parse(string? value) => value switch
    {
        "Account" => SettingsTab.Account,
        "SyncServer" => SettingsTab.SyncServer,
        "Startup" => SettingsTab.Startup,
        "Export" => SettingsTab.Export,
        "About" => SettingsTab.About,
        _ => SettingsTab.Startup
    };

    public static string ToStored(SettingsTab tab) => Clamp(tab) switch
    {
        SettingsTab.Account => "Account",
        SettingsTab.SyncServer => "SyncServer",
        SettingsTab.Startup => "Startup",
        SettingsTab.Export => "Export",
        SettingsTab.About => "About",
        _ => "Startup"
    };

    public static int ToIndex(SettingsTab tab) => Clamp(tab) switch
    {
        SettingsTab.Startup => 0,
        SettingsTab.Account => 1,
        SettingsTab.SyncServer => 2,
        SettingsTab.Export => 3,
        SettingsTab.About => 4,
        _ => 0
    };

    public static SettingsTab FromIndex(int index) => index switch
    {
        0 => SettingsTab.Startup,
        1 => SettingsTab.Account,
        2 => SettingsTab.SyncServer,
        3 => SettingsTab.Export,
        4 => SettingsTab.About,
        _ => SettingsTab.Startup
    };
}
