namespace CursorPace.Models;

public static class SyncInterval
{
    public static readonly int[] AllowedHours = [1, 2, 4, 6, 12];

    public static int Clamp(int hours) =>
        AllowedHours.Contains(hours) ? hours : 1;
}
