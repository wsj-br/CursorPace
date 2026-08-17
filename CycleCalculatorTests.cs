using CursorQuotaProgress.Models;
using CursorQuotaProgress.Services;
using Xunit;

namespace CursorQuotaProgress.Tests;

public class CycleCalculatorTests
{
    private readonly CycleCalculator _calculator = new();

    [Fact]
    public void FindCycleStart_CurrentMonthHasDay_ReturnsCurrentMonth()
    {
        var reference = new DateTime(2026, 1, 25);
        var result = _calculator.FindCycleStart(15, reference);

        Assert.Equal(new DateTime(2026, 1, 15), result);
    }

    [Fact]
    public void FindCycleStart_CurrentMonthBeforeRenewal_ReturnsPreviousValidMonth()
    {
        var reference = new DateTime(2026, 1, 10);
        var result = _calculator.FindCycleStart(15, reference);

        Assert.Equal(new DateTime(2025, 12, 15), result);
    }

    [Fact]
    public void FindCycleStart_Day31InFebruary_SkipsToJanuary()
    {
        var reference = new DateTime(2026, 2, 15);
        var result = _calculator.FindCycleStart(31, reference);

        Assert.Equal(new DateTime(2026, 1, 31), result);
    }

    [Fact]
    public void FindNextRenewal_SkipsMonthsWithoutDay()
    {
        var cycleStart = new DateTime(2026, 1, 31);
        var result = _calculator.FindNextRenewal(31, cycleStart);

        Assert.Equal(new DateTime(2026, 3, 31), result);
    }

    [Fact]
    public void FindNextRenewal_LeapYearFebruary29()
    {
        var cycleStart = new DateTime(2024, 1, 29);
        var result = _calculator.FindNextRenewal(29, cycleStart);

        Assert.Equal(new DateTime(2024, 2, 29), result);
    }

    [Fact]
    public void GenerateCycle_30DayCycle_CorrectDailyIncrement()
    {
        var cycle = _calculator.GenerateCycle(15, new DateTime(2026, 1, 20));

        Assert.Equal(31, cycle.Days.Count); // Jan 15 to Feb 15 is 31 days
        Assert.Equal(new DateTime(2026, 1, 15), cycle.CycleStart);
        Assert.Equal(new DateTime(2026, 2, 15), cycle.NextRenewal);

        Assert.Equal(0m, cycle.Days[0].CursorModelsPercent);
        Assert.Equal(100m * 1 / 31, cycle.Days[1].CursorModelsPercent);
        Assert.Equal(100m * 30 / 31, cycle.Days[30].CursorModelsPercent);
    }

    [Fact]
    public void GenerateCycle_31DayCycle_SkipsFebruary()
    {
        var cycle = _calculator.GenerateCycle(31, new DateTime(2026, 2, 10));

        Assert.Equal(new DateTime(2026, 1, 31), cycle.CycleStart);
        Assert.Equal(new DateTime(2026, 3, 31), cycle.NextRenewal);
        Assert.Equal(59, cycle.Days.Count);
    }

    [Fact]
    public void RecalculateQuota_EditDay15Of31_CorrectRemainingCalculation()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        cycle.Days[14].CursorModelsPercent = 35m;

        _calculator.RecalculateQuota(cycle, QuotaKind.CursorModels, 15);

        var remaining = 31 - 14;
        var increment = (100m - 35m) / remaining;

        Assert.Equal(35m + increment, cycle.Days[15].CursorModelsPercent, precision: 10);
        Assert.Equal(35m + 16 * increment, cycle.Days[30].CursorModelsPercent, precision: 10);
        Assert.False(cycle.Days[15].CursorModelsIsManual);
    }

    [Fact]
    public void RecalculateQuota_IndependentQuotas_OnlyAffectsSpecifiedKind()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        var originalOther = cycle.Days[15].OtherModelsPercent;

        cycle.Days[10].CursorModelsPercent = 50m;
        _calculator.RecalculateQuota(cycle, QuotaKind.CursorModels, 11);

        Assert.Equal(originalOther, cycle.Days[15].OtherModelsPercent);
    }
}

