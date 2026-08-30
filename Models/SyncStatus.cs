namespace CursorPace.Models;

public enum SyncStatus
{
    Idle,
    Syncing,
    SignedOut,
    AuthRequired,
    RateLimited,
    Error,
    Ok
}
