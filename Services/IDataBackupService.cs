namespace CursorUsageProgress.Services;

public interface IDataBackupService
{
    void WriteBackup(Stream destination, DateTimeOffset createdUtc);
    DataRestoreResult RestoreBackup(Stream source);
}

public sealed class DataRestoreResult
{
    public bool Success { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static DataRestoreResult Ok() => new() { Success = true };

    public static DataRestoreResult Fail(string message) =>
        new()
        {
            Success = false,
            ErrorMessage = message
        };
}
