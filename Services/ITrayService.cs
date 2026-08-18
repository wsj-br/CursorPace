namespace CursorUsageProgress.Services;

public interface ITrayService
{
    void Initialize(Action onOpenRequested, Action onQuitRequested);
    void UpdateToolTip(string text);
    void ShowWindow();
    void Dispose();
}
