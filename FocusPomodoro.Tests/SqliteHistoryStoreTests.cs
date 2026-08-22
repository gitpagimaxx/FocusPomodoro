using FocusPomodoro.Models;
using FocusPomodoro.Services;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class SqliteHistoryStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "FocusPomodoroTests",
        Guid.NewGuid().ToString("N"));

    public SqliteHistoryStoreTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task Initialize_CreatesSchemaAndUserVersion1()
    {
        var store = CreateStore();

        await store.InitializeAsync();

        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={DbPath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        Assert.Equal(1L, (long)(command.ExecuteScalar() ?? 0L));
    }

    [Fact]
    public async Task OpenAndClose_RoundTripsCompletedLog()
    {
        var store = CreateStore();
        await store.InitializeAsync();
        var started = new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);

        var opened = await store.OpenPhaseAsync(
            PomodoroPhase.Focus,
            cycle: 2,
            TimeSpan.FromMinutes(25),
            started);
        await store.ClosePhaseAsync(
            opened.Id,
            started.AddMinutes(25),
            TimeSpan.FromMinutes(25),
            PhaseOutcome.Completed);

        var logs = await store.GetLogsAsync();
        var log = Assert.Single(logs);
        Assert.Equal(PomodoroPhase.Focus, log.Phase);
        Assert.Equal(2, log.Cycle);
        Assert.Equal(started, log.StartedAt);
        Assert.Equal(started.AddMinutes(25), log.EndedAt);
        Assert.Equal(TimeSpan.FromMinutes(25), log.PlannedDuration);
        Assert.Equal(TimeSpan.FromMinutes(25), log.Elapsed);
        Assert.Equal(PhaseOutcome.Completed, log.Outcome);
        Assert.Null(await store.GetInProgressAsync());
    }

    [Fact]
    public async Task SaveAndLoadSnapshot_RoundTrips()
    {
        var store = CreateStore();
        await store.InitializeAsync();
        var snapshot = new SessionSnapshot
        {
            Phase = PomodoroPhase.ShortBreak,
            Cycle = 3,
            Remaining = TimeSpan.FromMinutes(4),
            TotalPhaseDuration = TimeSpan.FromMinutes(5),
            IsRunning = true,
            IsPaused = false,
            UpdatedAt = new DateTimeOffset(2026, 8, 21, 15, 30, 0, TimeSpan.Zero)
        };

        await store.SaveSnapshotAsync(snapshot);
        var loaded = await store.LoadSnapshotAsync();

        Assert.NotNull(loaded);
        Assert.Equal(snapshot.Phase, loaded.Phase);
        Assert.Equal(snapshot.Cycle, loaded.Cycle);
        Assert.Equal(snapshot.Remaining, loaded.Remaining);
        Assert.Equal(snapshot.TotalPhaseDuration, loaded.TotalPhaseDuration);
        Assert.True(loaded.IsRunning);
        Assert.False(loaded.IsPaused);
        Assert.Equal(snapshot.UpdatedAt, loaded.UpdatedAt);
    }

    [Fact]
    public async Task GetInProgress_ReturnsOpenRow()
    {
        var store = CreateStore();
        await store.InitializeAsync();
        var opened = await store.OpenPhaseAsync(
            PomodoroPhase.Focus,
            1,
            TimeSpan.FromMinutes(25),
            DateTimeOffset.UnixEpoch);

        var inProgress = await store.GetInProgressAsync();

        Assert.NotNull(inProgress);
        Assert.Equal(opened.Id, inProgress.Id);
        Assert.Equal(PhaseOutcome.InProgress, inProgress.Outcome);
    }

    [Fact]
    public async Task Initialize_WhenMultipleInProgress_KeepsLatestInterruptsOlder()
    {
        var store = CreateStore();
        await store.InitializeAsync();
        var first = await store.OpenPhaseAsync(
            PomodoroPhase.Focus, 1, TimeSpan.FromMinutes(25), DateTimeOffset.UnixEpoch);
        var second = await store.OpenPhaseAsync(
            PomodoroPhase.Focus, 1, TimeSpan.FromMinutes(25), DateTimeOffset.UnixEpoch.AddMinutes(1));

        await store.InitializeAsync();

        var inProgress = await store.GetInProgressAsync();
        Assert.NotNull(inProgress);
        Assert.Equal(second.Id, inProgress.Id);
        var logs = await store.GetLogsAsync();
        Assert.Equal(PhaseOutcome.Interrupted, logs.Single(log => log.Id == first.Id).Outcome);
    }

    [Fact]
    public async Task Initialize_WhenFileIsGarbage_RenamesBakAndCreatesFresh()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(DbPath, "not a sqlite database");
        var store = CreateStore();

        await store.InitializeAsync();
        var opened = await store.OpenPhaseAsync(
            PomodoroPhase.Focus, 1, TimeSpan.FromMinutes(25), DateTimeOffset.UnixEpoch);

        Assert.True(File.Exists(DbPath + ".bak"));
        Assert.Equal("not a sqlite database", await File.ReadAllTextAsync(DbPath + ".bak"));
        Assert.True(opened.Id > 0);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }

    private string DbPath => Path.Combine(_directory, SqliteHistoryStore.FileName);

    private SqliteHistoryStore CreateStore() => new(DbPath);
}
