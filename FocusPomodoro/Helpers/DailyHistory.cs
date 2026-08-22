using FocusPomodoro.Models;

namespace FocusPomodoro.Helpers;

public sealed class DailyHistoryGroup
{
    public DailyHistoryGroup(
        DateOnly date,
        int completedFocusCount,
        TimeSpan completedFocusElapsed,
        IReadOnlyList<PhaseLog> entries)
    {
        Date = date;
        CompletedFocusCount = completedFocusCount;
        CompletedFocusElapsed = completedFocusElapsed;
        Entries = entries;
    }

    public DateOnly Date { get; }
    public int CompletedFocusCount { get; }
    public TimeSpan CompletedFocusElapsed { get; }
    public IReadOnlyList<PhaseLog> Entries { get; }
}

public static class DailyHistory
{
    public static IReadOnlyList<DailyHistoryGroup> GroupByLocalDay(
        IEnumerable<PhaseLog> logs,
        TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(logs);
        ArgumentNullException.ThrowIfNull(timeZone);

        return logs
            .GroupBy(log => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(log.StartedAt, timeZone).DateTime))
            .OrderByDescending(group => group.Key)
            .Select(group =>
            {
                var entries = group
                    .OrderByDescending(log => log.StartedAt)
                    .ToArray();
                var completedFocus = entries
                    .Where(log => log.Phase == PomodoroPhase.Focus && log.Outcome == PhaseOutcome.Completed)
                    .ToArray();
                var elapsed = completedFocus.Aggregate(TimeSpan.Zero, (sum, log) => sum + log.Elapsed);

                return new DailyHistoryGroup(
                    group.Key,
                    completedFocus.Length,
                    elapsed,
                    entries);
            })
            .ToArray();
    }
}
