using System.Diagnostics;

namespace CursorPace.Services;

public static class FolderOpener
{
    public static bool TryOpenContainingFolder(string filePath, out string? error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                error = "Could not determine the destination folder.";
                return false;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows()
                    ? "explorer.exe"
                    : OperatingSystem.IsMacOS()
                        ? "open"
                        : "xdg-open",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add(directory);

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                error = "Could not open the destination folder.";
                return false;
            }

            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = string.IsNullOrWhiteSpace(ex.Message)
                ? "Could not open the destination folder."
                : ex.Message;
            return false;
        }
    }
}
