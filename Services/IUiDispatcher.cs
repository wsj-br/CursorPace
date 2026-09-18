namespace CursorPace.Services;

public interface IUiDispatcher
{
    bool CheckAccess();
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
