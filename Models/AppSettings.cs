namespace CursorUsageProgress.Models;

public sealed class AppSettings
{
    public int Version { get; set; } = 1;
    public int? RenewalDay { get; set; }
    public QuotaCycle? ActiveCycle { get; set; }
    public bool RunAtStartup { get; set; }
}
