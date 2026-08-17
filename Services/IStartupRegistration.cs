namespace CursorQuotaProgress.Services;

public interface IStartupRegistration
{
    bool IsRegistered { get; }
    void Register();
    void Unregister();
}
