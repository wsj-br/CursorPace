namespace CursorQuotaProgress.Services;

public interface IClock
{
    DateTime Now { get; }
    DateTime Today { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
    public DateTime Today => DateTime.Today;
}
