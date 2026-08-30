namespace CursorUsageProgress.Services;

public interface IUiDispatcher
{
    void Post(Action action);
    IUiTimer CreateTimer();
}

public interface IUiTimer
{
    TimeSpan Interval { get; set; }
    bool IsRepeating { get; set; }
    event EventHandler? Tick;
    void Start();
    void Stop();
}
