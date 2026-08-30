namespace CursorPace.Services;

public interface ISingleInstance : IDisposable
{
    bool TryAcquire();
    void Listen(Action onActivated);
    void SignalExisting();
}
