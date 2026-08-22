using System.Globalization;
using FocusPomodoro.Models;
using Microsoft.Data.Sqlite;

namespace FocusPomodoro.Services;

public sealed class SqliteHistoryStore : IHistoryStore
{
    public const string FileName = "pomodoro.db";
    public const int SchemaVersion = 1;

    private readonly string _filePath;

    public SqliteHistoryStore(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSchemaAsync(connection, cancellationToken).ConfigureAwait(false);
            await HealInProgressAsync(connection, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            RecoverCorruptFile();
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSchemaAsync(connection, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<PhaseLog> OpenPhaseAsync(
        PomodoroPhase phase,
        int cycle,
        TimeSpan planned,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO phase_logs (phase, cycle, started_at, ended_at, planned_duration_ms, elapsed_ms, outcome)
            VALUES ($phase, $cycle, $started, NULL, $planned, 0, $outcome);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$phase", phase.ToString());
        command.Parameters.AddWithValue("$cycle", cycle);
        command.Parameters.AddWithValue("$started", FormatTime(startedAt));
        command.Parameters.AddWithValue("$planned", (long)planned.TotalMilliseconds);
        command.Parameters.AddWithValue("$outcome", PhaseOutcome.InProgress.ToString());
        var id = (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? 0L);

        return new PhaseLog
        {
            Id = id,
            Phase = phase,
            Cycle = cycle,
            StartedAt = startedAt,
            PlannedDuration = planned,
            Elapsed = TimeSpan.Zero,
            Outcome = PhaseOutcome.InProgress
        };
    }

    public async Task ClosePhaseAsync(
        long id,
        DateTimeOffset endedAt,
        TimeSpan elapsed,
        PhaseOutcome outcome,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE phase_logs
            SET ended_at = $ended, elapsed_ms = $elapsed, outcome = $outcome
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$ended", FormatTime(endedAt));
        command.Parameters.AddWithValue("$elapsed", (long)elapsed.TotalMilliseconds);
        command.Parameters.AddWithValue("$outcome", outcome.ToString());
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PhaseLog?> GetInProgressAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, phase, cycle, started_at, ended_at, planned_duration_ms, elapsed_ms, outcome
            FROM phase_logs
            WHERE outcome = $outcome
            ORDER BY id DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$outcome", PhaseOutcome.InProgress.ToString());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ReadLog(reader) : null;
    }

    public async Task<IReadOnlyList<PhaseLog>> GetLogsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, phase, cycle, started_at, ended_at, planned_duration_ms, elapsed_ms, outcome
            FROM phase_logs
            ORDER BY started_at DESC, id DESC;
            """;
        var logs = new List<PhaseLog>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            logs.Add(ReadLog(reader));
        }

        return logs;
    }

    public async Task SaveSnapshotAsync(SessionSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO session_state (id, phase, cycle, remaining_ms, total_duration_ms, is_running, is_paused, updated_at)
            VALUES (1, $phase, $cycle, $remaining, $total, $running, $paused, $updated)
            ON CONFLICT(id) DO UPDATE SET
                phase = excluded.phase,
                cycle = excluded.cycle,
                remaining_ms = excluded.remaining_ms,
                total_duration_ms = excluded.total_duration_ms,
                is_running = excluded.is_running,
                is_paused = excluded.is_paused,
                updated_at = excluded.updated_at;
            """;
        command.Parameters.AddWithValue("$phase", snapshot.Phase.ToString());
        command.Parameters.AddWithValue("$cycle", snapshot.Cycle);
        command.Parameters.AddWithValue("$remaining", (long)snapshot.Remaining.TotalMilliseconds);
        command.Parameters.AddWithValue("$total", (long)snapshot.TotalPhaseDuration.TotalMilliseconds);
        command.Parameters.AddWithValue("$running", snapshot.IsRunning ? 1 : 0);
        command.Parameters.AddWithValue("$paused", snapshot.IsPaused ? 1 : 0);
        command.Parameters.AddWithValue("$updated", FormatTime(snapshot.UpdatedAt));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<SessionSnapshot?> LoadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT phase, cycle, remaining_ms, total_duration_ms, is_running, is_paused, updated_at
            FROM session_state
            WHERE id = 1;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new SessionSnapshot
        {
            Phase = Enum.Parse<PomodoroPhase>(reader.GetString(0)),
            Cycle = reader.GetInt32(1),
            Remaining = TimeSpan.FromMilliseconds(reader.GetInt64(2)),
            TotalPhaseDuration = TimeSpan.FromMilliseconds(reader.GetInt64(3)),
            IsRunning = reader.GetInt32(4) != 0,
            IsPaused = reader.GetInt32(5) != 0,
            UpdatedAt = ParseTime(reader.GetString(6))
        };
    }

    private SqliteConnection CreateConnection() => new($"Data Source={_filePath}");

    private static async Task EnsureSchemaAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS phase_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                phase TEXT NOT NULL,
                cycle INTEGER NOT NULL,
                started_at TEXT NOT NULL,
                ended_at TEXT NULL,
                planned_duration_ms INTEGER NOT NULL,
                elapsed_ms INTEGER NOT NULL DEFAULT 0,
                outcome TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_phase_logs_started_at ON phase_logs(started_at);
            CREATE TABLE IF NOT EXISTS session_state (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                phase TEXT NOT NULL,
                cycle INTEGER NOT NULL,
                remaining_ms INTEGER NOT NULL,
                total_duration_ms INTEGER NOT NULL,
                is_running INTEGER NOT NULL,
                is_paused INTEGER NOT NULL,
                updated_at TEXT NOT NULL
            );
            PRAGMA user_version = 1;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task HealInProgressAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var select = connection.CreateCommand();
        select.CommandText =
            """
            SELECT id FROM phase_logs
            WHERE outcome = $outcome
            ORDER BY id DESC;
            """;
        select.Parameters.AddWithValue("$outcome", PhaseOutcome.InProgress.ToString());
        var ids = new List<long>();
        await using (var reader = await select.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                ids.Add(reader.GetInt64(0));
            }
        }

        if (ids.Count <= 1)
        {
            return;
        }

        var now = FormatTime(DateTimeOffset.UtcNow);
        foreach (var id in ids.Skip(1))
        {
            await using var update = connection.CreateCommand();
            update.CommandText =
                """
                UPDATE phase_logs
                SET outcome = $outcome, ended_at = $ended
                WHERE id = $id;
                """;
            update.Parameters.AddWithValue("$outcome", PhaseOutcome.Interrupted.ToString());
            update.Parameters.AddWithValue("$ended", now);
            update.Parameters.AddWithValue("$id", id);
            await update.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private void RecoverCorruptFile()
    {
        var backupPath = _filePath + ".bak";
        SqliteConnection.ClearAllPools();
        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }

        if (File.Exists(_filePath))
        {
            File.Move(_filePath, backupPath);
        }
    }

    private static PhaseLog ReadLog(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Phase = Enum.Parse<PomodoroPhase>(reader.GetString(1)),
        Cycle = reader.GetInt32(2),
        StartedAt = ParseTime(reader.GetString(3)),
        EndedAt = reader.IsDBNull(4) ? null : ParseTime(reader.GetString(4)),
        PlannedDuration = TimeSpan.FromMilliseconds(reader.GetInt64(5)),
        Elapsed = TimeSpan.FromMilliseconds(reader.GetInt64(6)),
        Outcome = Enum.Parse<PhaseOutcome>(reader.GetString(7))
    };

    private static string FormatTime(DateTimeOffset value) => value.ToString("o", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseTime(string value) => DateTimeOffset.Parse(
        value,
        CultureInfo.InvariantCulture,
        DateTimeStyles.RoundtripKind);
}
