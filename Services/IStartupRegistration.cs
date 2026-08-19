namespace CursorUsageProgress.Services;

public interface IStartupRegistration
{
    bool IsRegistered { get; }
    void Register(bool startInTray);
    void Unregister();
}
