using FocusPomodoro.Helpers;
using FocusPomodoro.Models;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class HistoryPresentationTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    [Fact]
    public void OutcomeText_MapsAllResults()
    {
        Assert.Equal("Em andamento", HistoryPresentation.OutcomeText(PhaseOutcome.InProgress));
        Assert.Equal("Concluída", HistoryPresentation.OutcomeText(PhaseOutcome.Completed));
        Assert.Equal("Pulada", HistoryPresentation.OutcomeText(PhaseOutcome.Skipped));
        Assert.Equal("Interrompida", HistoryPresentation.OutcomeText(PhaseOutcome.Interrupted));
    }

    [Fact]
    public void ContinuePrompt_IncludesFormattedRemaining()
    {
        Assert.Equal(
            "Continuar 12:40 restantes?",
            HistoryPresentation.ContinuePrompt(TimeSpan.FromMinutes(12) + TimeSpan.FromSeconds(40)));
    }

    [Fact]
    public void Line_IncludesLocalTimePhaseDurationAndOutcome()
    {
        var log = new PhaseLog
        {
            Phase = PomodoroPhase.Focus,
            StartedAt = new DateTimeOffset(2026, 8, 21, 12, 5, 0, TimeSpan.Zero),
            Elapsed = TimeSpan.FromMinutes(25),
            Outcome = PhaseOutcome.Completed
        };

        Assert.Equal(
            "12:05  Foco  25:00  Concluída",
            HistoryPresentation.Line(log, Utc));
    }

    [Fact]
    public void DayHeader_CountsCompletedFocusAndElapsed()
    {
        var group = new DailyHistoryGroup(
            new DateOnly(2026, 8, 21),
            completedFocusCount: 4,
            completedFocusElapsed: TimeSpan.FromMinutes(100),
            entries: []);

        Assert.Equal("21/08 · 4 focos · 100:00", HistoryPresentation.DayHeader(group));
    }
}
