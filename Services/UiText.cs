using System.Globalization;

namespace CursorPace.Services;

public static class UiText
{
    public static string Translate(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return message ?? "";

        var http = TranslateHttp(message);
        if (http != null)
            return http;

        return message switch
        {
            "Could not sync with the server." => T("Could not sync with the server."),
            "Could not reach the sync server." => T("Could not reach the sync server."),
            "The sync server did not answer in time." => T("The sync server did not answer in time."),
            "The sync server rejected the API token." => T("The sync server rejected the API token."),
            "The sync server returned an unreadable response." => T("The sync server returned an unreadable response."),
            "The sync server returned an empty response." => T("The sync server returned an empty response."),
            "Set the sync server URL first." => T("Set the sync server URL first."),
            "The sync server URL must start with http:// or https://." => T("The sync server URL must start with http:// or https://."),
            "Sign in to Cursor to sync usage." => T("Sign in to Cursor to sync usage."),
            "Sign in was cancelled." => T("Sign in was cancelled."),
            "Cursor rate-limited the request. Will retry later." => T("Cursor rate-limited the request. Will retry later."),
            "Could not update usage." => T("Could not update usage."),
            "The Cursor usage response could not be parsed." => T("The Cursor usage response could not be parsed."),
            "Could not start the embedded browser. Install WebKitGTK 4.1 (and WPE WebKit if your distribution requires it)." =>
                T("Could not start the embedded browser. Install WebKitGTK 4.1 (and WPE WebKit if your distribution requires it)."),
            "Could not start the embedded browser used to sign in to Cursor." =>
                T("Could not start the embedded browser used to sign in to Cursor."),
            "The usage request was cancelled." => T("The usage request was cancelled."),
            "The backup file does not contain settings.json." => T("The backup file does not contain settings.json."),
            "This backup was created by a newer app version." => T("This backup was created by a newer app version."),
            "This file is not a CursorPace backup." => T("This file is not a CursorPace backup."),
            "This file is not a valid backup archive." => T("This file is not a valid backup archive."),
            "Could not read the backup file." => T("Could not read the backup file."),
            "The backup settings.json is not valid." => T("The backup settings.json is not valid."),
            "The backup usage-samples.json is not valid." => T("The backup usage-samples.json is not valid."),
            "Could not restore usage samples." => T("Could not restore usage samples."),
            "Could not restore the backup file." => T("Could not restore the backup file."),
            "Could not write the backup file." => T("Could not write the backup file."),
            "Could not determine the destination folder." => T("Could not determine the destination folder."),
            "Could not open the destination folder." => T("Could not open the destination folder."),
            _ => message
        };
    }

    private static string? TranslateHttp(string message)
    {
        const string prefix = "The sync server returned ";
        if (!message.StartsWith(prefix, StringComparison.Ordinal) || !message.EndsWith('.'))
            return null;

        var rest = message[prefix.Length..^1];
        const string separator = " for ";
        var index = rest.IndexOf(separator, StringComparison.Ordinal);
        if (index <= 0)
            return null;

        var number = rest[..index];
        var operation = rest[(index + separator.Length)..];
        if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var status))
            return null;

        return operation switch
        {
            "push" => T("The sync server returned {0} for push.", status),
            "pull" => T("The sync server returned {0} for pull.", status),
            _ => T("The sync server returned {0} for {1}.", status, operation)
        };
    }
}
