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
    public void ExpectedPercent_WithNoEdits_MatchesLinear()
    {
        var cycle = _calculator.GenerateCycle(15, new DateTime(2026, 1, 20));
        var totalDays = cycle.Days.Count;

        Assert.Equal(0m, _calculator.ExpectedPercent(cycle, QuotaKind.CursorModels, 1));
        Assert.Equal(100m * 1 / totalDays, _calculator.ExpectedPercent(cycle, QuotaKind.CursorModels, 2));
        Assert.Equal(100m * (totalDays - 1) / totalDays, _calculator.ExpectedPercent(cycle, QuotaKind.CursorModels, totalDays));
    }

    [Fact]
    public void SetManual_ComputesRemainingDaysFromEdit()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        _calculator.SetManual(cycle, QuotaKind.CursorModels, 15, 35m);

        var remaining = 31 - 14;
        var increment = (100m - 35m) / remaining;

        Assert.Equal(35m, cycle.Days[14].CursorModelsPercent);
        Assert.True(cycle.Days[14].CursorModelsIsManual);
        Assert.Equal(35m + increment, cycle.Days[15].CursorModelsPercent, precision: 10);
        Assert.Equal(35m + 16 * increment, cycle.Days[30].CursorModelsPercent, precision: 10);
        Assert.False(cycle.Days[15].CursorModelsIsManual);
        Assert.Single(cycle.Edits);
    }

    [Fact]
    public void SetManual_WithLaterEdit_InterpolatesUntilThatDayThenToRenewal()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        _calculator.SetManual(cycle, QuotaKind.CursorModels, 10, 20m);
        _calculator.SetManual(cycle, QuotaKind.CursorModels, 20, 50m);

        Assert.Equal(20m, cycle.Days[9].CursorModelsPercent);
        Assert.Equal(50m, cycle.Days[19].CursorModelsPercent);

        // Days 11-19 interpolate 20% → 50% over 10 day-steps
        Assert.Equal(20m + 5m * 30m / 10m, cycle.Days[14].CursorModelsPercent, precision: 10);

        // Days after 20 interpolate 50% → 100% until renewal
        var remaining = 31 - 19;
        var increment = (100m - 50m) / remaining;
        Assert.Equal(50m + increment, cycle.Days[20].CursorModelsPercent, precision: 10);
        Assert.False(cycle.Days[14].CursorModelsIsManual);
        Assert.False(cycle.Days[20].CursorModelsIsManual);
    }

    [Fact]
    public void SetManual_DaysBeforeFirstEdit_InterpolateTowardThatEdit()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        _calculator.SetManual(cycle, QuotaKind.CursorModels, 20, 50m);

        Assert.Equal(50m, cycle.Days[19].CursorModelsPercent);
        Assert.True(cycle.Days[19].CursorModelsIsManual);

        // Day 10 sits on 0% (day 1) → 50% (day 20), not on the full-cycle linear line
        Assert.Equal(9m * 50m / 19m, cycle.Days[9].CursorModelsPercent, precision: 10);
        Assert.Equal(18m * 50m / 19m, cycle.Days[18].CursorModelsPercent, precision: 10);
        Assert.False(cycle.Days[18].CursorModelsIsManual);
    }

    [Fact]
    public void SetManual_IndependentQuotas_OnlyAffectsSpecifiedKind()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        var originalOther = cycle.Days[15].OtherModelsPercent;

        _calculator.SetManual(cycle, QuotaKind.CursorModels, 11, 50m);

        Assert.Equal(originalOther, cycle.Days[15].OtherModelsPercent);
        Assert.Null(cycle.Edits.Single().OtherModelsPercent);
    }

    [Fact]
    public void ClearManual_RestoresLinearWhenNoPriorEdit()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        var original = cycle.Days[14].CursorModelsPercent;

        _calculator.SetManual(cycle, QuotaKind.CursorModels, 15, 35m);
        _calculator.ClearManual(cycle, QuotaKind.CursorModels, 15);

        Assert.Equal(original, cycle.Days[14].CursorModelsPercent);
        Assert.False(cycle.Days[14].CursorModelsIsManual);
        Assert.Empty(cycle.Edits);
    }

    [Fact]
    public void ClearManual_UsesPriorEditForExpectedPercent()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        _calculator.SetManual(cycle, QuotaKind.CursorModels, 10, 20m);
        _calculator.SetManual(cycle, QuotaKind.CursorModels, 20, 80m);

        _calculator.ClearManual(cycle, QuotaKind.CursorModels, 20);

        var remaining = 31 - 9;
        var increment = (100m - 20m) / remaining;
        Assert.Equal(20m + 10 * increment, cycle.Days[19].CursorModelsPercent, precision: 10);
        Assert.False(cycle.Days[19].CursorModelsIsManual);
        Assert.Single(cycle.Edits);
    }
}
