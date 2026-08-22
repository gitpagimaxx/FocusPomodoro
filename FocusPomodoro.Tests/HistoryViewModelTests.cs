using FocusPomodoro.Models;
using FocusPomodoro.ViewModels;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class HistoryViewModelTests
{
    [Fact]
    public async Task LoadAsync_GroupsLogsByLocalDay()
    {
        var store = new FakeHistoryStore();
        store.Logs.Add(new PhaseLog
        {
            Id = 1,
            Phase = PomodoroPhase.Focus,
            Cycle = 1,
            StartedAt = new DateTimeOffset(2026, 8, 21, 15, 0, 0, TimeSpan.Zero),
            Elapsed = TimeSpan.FromMinutes(25),
            PlannedDuration = TimeSpan.FromMinutes(25),
            Outcome = PhaseOutcome.Completed
        });
        var viewModel = new HistoryViewModel(store, TimeZoneInfo.Utc);

        await viewModel.LoadAsync();

        var day = Assert.Single(viewModel.Days);
        Assert.Contains("1 focos", day.Header);
        Assert.Single(day.Lines);
    }

    [Fact]
    public async Task LoadAsync_WhenStoreEmpty_HasNoDays()
    {
        var viewModel = new HistoryViewModel(new FakeHistoryStore(), TimeZoneInfo.Utc);

        await viewModel.LoadAsync();

        Assert.Empty(viewModel.Days);
    }
}
