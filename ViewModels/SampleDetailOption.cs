using CursorPace.Models;

namespace CursorPace.ViewModels;

public sealed class SampleDetailOption
{
    public SampleDetailOption(SampleDetailChoice choice) => Choice = choice;

    public SampleDetailChoice Choice { get; }

    public int Days => Choice.Days;

    public override string ToString() => Days switch
    {
        2 => T("2D"),
        4 => T("4D"),
        7 => T("7D"),
        14 => T("14D"),
        _ => throw new ArgumentOutOfRangeException(nameof(Days), Days, null)
    };

    public override bool Equals(object? obj) => obj is SampleDetailOption other && other.Days == Days;

    public override int GetHashCode() => Days;
}
