using FocusPomodoro.Helpers;
using FocusPomodoro.Models;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class DailyHistoryTests
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.CreateCustomTimeZone(
        "TestTz",
        TimeSpan.FromHours(-3),
        "TestTz",
        "TestTz");

    [Fact]
    public void GroupByLocalDay_GroupsByLocalDateNewestFirst_AndCountsCompletedFocusOnly()
    {
        var lateUtc = new DateTimeOffset(2026, 8, 22, 2, 0, 0, TimeSpan.Zero);
        var earlierSameLocalDay = new DateTimeOffset(2026, 8, 21, 15, 0, 0, TimeSpan.Zero);
        var previousLocalDay = new DateTimeOffset(2026, 8, 21, 2, 0, 0, TimeSpan.Zero);

        var logs = new[]
        {
            Log(1, PomodoroPhase.Focus, PhaseOutcome.Completed, lateUtc, TimeSpan.FromMinutes(25)),
            Log(2, PomodoroPhase.ShortBreak, PhaseOutcome.Completed, earlierSameLocalDay, TimeSpan.FromMinutes(5)),
            Log(3, PomodoroPhase.Focus, PhaseOutcome.Skipped, earlierSameLocalDay, TimeSpan.FromMinutes(10)),
            Log(4, PomodoroPhase.Focus, PhaseOutcome.Completed, previousLocalDay, TimeSpan.FromMinutes(20)),
        };

        var groups = DailyHistory.GroupByLocalDay(logs, Tz);

        Assert.Equal(2, groups.Count);
        Assert.Equal(new DateOnly(2026, 8, 21), groups[0].Date);
        Assert.Equal(1, groups[0].CompletedFocusCount);
        Assert.Equal(TimeSpan.FromMinutes(25), groups[0].CompletedFocusElapsed);
        Assert.Equal(3, groups[0].Entries.Count);

        Assert.Equal(new DateOnly(2026, 8, 20), groups[1].Date);
        Assert.Equal(1, groups[1].CompletedFocusCount);
        Assert.Equal(TimeSpan.FromMinutes(20), groups[1].CompletedFocusElapsed);
        Assert.Single(groups[1].Entries);
    }

    private static PhaseLog Log(
        long id,
        PomodoroPhase phase,
        PhaseOutcome outcome,
        DateTimeOffset startedAt,
        TimeSpan elapsed) => new()
    {
        Id = id,
        Phase = phase,
        Cycle = 1,
        StartedAt = startedAt,
        EndedAt = startedAt + elapsed,
        PlannedDuration = TimeSpan.FromMinutes(25),
        Elapsed = elapsed,
        Outcome = outcome
    };
}
