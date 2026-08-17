namespace CursorQuotaProgress.Services;

public interface ITrayService
{
    void Initialize(Action onOpenRequested, Action onQuitRequested);
    void ShowWindow();
    void Dispose();
}
