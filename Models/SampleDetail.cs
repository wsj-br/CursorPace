namespace CursorPace.Models;

public static class SampleDetail
{
    public const int DefaultDays = 4;

    public static readonly int[] AllowedDays = [2, 4, 7, 14];

    public static readonly SampleDetailChoice[] Options =
    [
        new(2),
        new(4),
        new(7),
        new(14)
    ];

    public static int Clamp(int days) =>
        AllowedDays.Contains(days) ? days : DefaultDays;

    public static decimal MaxSeconds(int days) =>
        Clamp(days) * 24m * 60m * 60m;
}

public readonly record struct SampleDetailChoice(int Days)
{
    public string Label => Days switch
    {
        2 => "2D",
        4 => "4D",
        7 => "7D",
        14 => "14D",
        _ => throw new ArgumentOutOfRangeException(nameof(Days), Days, null)
    };

    public override string ToString() => Label;
}
