# Session History SQLite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist every Pomodoro phase in local SQLite and offer Continue / Start fresh after Exit, with a history panel beside the compact timer.

**Architecture:** `PomodoroTimerService` stays the clock. It gains `Checkpoint`, `Restore`, and `PhaseEndReason` on `PhaseTransition`. `SessionHistoryService` maps those events to `IHistoryStore` (`Microsoft.Data.Sqlite`, `pomodoro.db` in LocalFolder). `HistoryViewModel` only reads logs. Resume and the side panel are WinUI, tested via portable helpers.

**Tech Stack:** WinUI 3, .NET 10, `Microsoft.Data.Sqlite` 9.0.9, CommunityToolkit.Mvvm, xUnit. Test project keeps linking portable files.

**Do not commit** unless the user asks.

---

## File structure

- Create: `FocusPomodoro/Models/PhaseEndReason.cs`
- Create: `FocusPomodoro/Models/PhaseOutcome.cs`
- Create: `FocusPomodoro/Models/PhaseLog.cs`
- Create: `FocusPomodoro/Models/SessionSnapshot.cs`
- Create: `FocusPomodoro/Models/ContinueChoice.cs`
- Create: `FocusPomodoro/Helpers/PhaseElapsed.cs`
- Create: `FocusPomodoro/Helpers/DailyHistory.cs`
- Create: `FocusPomodoro/Helpers/HistoryPanelLayout.cs`
- Create: `FocusPomodoro/Helpers/HistoryPresentation.cs`
- Create: `FocusPomodoro/Services/IHistoryStore.cs`
- Create: `FocusPomodoro/Services/SqliteHistoryStore.cs`
- Create: `FocusPomodoro/Services/ISessionHistoryService.cs`
- Create: `FocusPomodoro/Services/SessionHistoryService.cs`
- Create: `FocusPomodoro/ViewModels/HistoryViewModel.cs`
- Create: `FocusPomodoro/ContinueChoiceWindow.xaml`
- Create: `FocusPomodoro/ContinueChoiceWindow.xaml.cs`
- Create: `FocusPomodoro/HistoryPanelWindow.xaml`
- Create: `FocusPomodoro/HistoryPanelWindow.xaml.cs`
- Create: `FocusPomodoro.Tests/PhaseElapsedTests.cs`
- Create: `FocusPomodoro.Tests/DailyHistoryTests.cs`
- Create: `FocusPomodoro.Tests/HistoryPanelLayoutTests.cs`
- Create: `FocusPomodoro.Tests/SqliteHistoryStoreTests.cs`
- Create: `FocusPomodoro.Tests/SessionHistoryServiceTests.cs`
- Create: `FocusPomodoro.Tests/HistoryPresentationTests.cs`
- Create: `FocusPomodoro.Tests/FakeHistoryStore.cs`
- Modify: `FocusPomodoro/Models/PhaseTransition.cs`
- Modify: `FocusPomodoro/Services/IPomodoroTimerService.cs`
- Modify: `FocusPomodoro/Services/PomodoroTimerService.cs`
- Modify: `FocusPomodoro/Services/NotificationService.cs`
- Modify: `FocusPomodoro/Services/SoundService.cs`
- Modify: `FocusPomodoro/ViewModels/MainViewModel.cs`
- Modify: `FocusPomodoro/MainWindow.xaml`
- Modify: `FocusPomodoro/MainWindow.xaml.cs`
- Modify: `FocusPomodoro/App.xaml.cs`
- Modify: `FocusPomodoro/FocusPomodoro.csproj`
- Modify: `FocusPomodoro.Tests/FocusPomodoro.Tests.csproj`
- Modify: `FocusPomodoro.Tests/PomodoroTimerServiceTests.cs`
- Modify: `FocusPomodoro.Tests/SettingsViewModelTests.cs` (fake timer)
- Modify: `FocusPomodoro.Tests/NotificationServiceTests.cs`
- Modify: `docs/superpowers/specs/2026-08-21-session-history-sqlite-design.md` (`total_duration_ms` on snapshot)

`session_state` columns: `id`, `phase`, `cycle`, `remaining_ms`, `total_duration_ms`, `is_running`, `is_paused`, `updated_at`.

---

### Task 1: Domain models and elapsed helper

**Files:**
- Create: `FocusPomodoro/Models/PhaseEndReason.cs`
- Create: `FocusPomodoro/Models/PhaseOutcome.cs`
- Create: `FocusPomodoro/Models/PhaseLog.cs`
- Create: `FocusPomodoro/Models/SessionSnapshot.cs`
- Create: `FocusPomodoro/Models/ContinueChoice.cs`
- Create: `FocusPomodoro/Helpers/PhaseElapsed.cs`
- Modify: `FocusPomodoro/Models/PhaseTransition.cs`
- Test: `FocusPomodoro.Tests/PhaseElapsedTests.cs`
- Modify: `FocusPomodoro.Tests/FocusPomodoro.Tests.csproj` (link new files)

- [ ] **Step 1: Write failing PhaseElapsed tests**

```csharp
using FocusPomodoro.Helpers;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class PhaseElapsedTests
{
    [Fact]
    public void FromRemaining_SubtractsRemainingAndClampsToPlanned()
    {
        Assert.Equal(TimeSpan.FromMinutes(10), PhaseElapsed.FromRemaining(
            TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(15)));
        Assert.Equal(TimeSpan.Zero, PhaseElapsed.FromRemaining(
            TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(30)));
        Assert.Equal(TimeSpan.FromMinutes(25), PhaseElapsed.FromRemaining(
            TimeSpan.FromMinutes(25), TimeSpan.Zero));
        Assert.Equal(TimeSpan.Zero, PhaseElapsed.FromRemaining(TimeSpan.Zero, TimeSpan.Zero));
    }
}
```

- [ ] **Step 2: Run test — expect compile fail (type missing)**

Run: `dotnet test FocusPomodoro.Tests/FocusPomodoro.Tests.csproj --filter PhaseElapsedTests`

- [ ] **Step 3: Add models + helper; link them in the test csproj**

PhaseEndReason: `Completed`, `Skipped`, `Interrupted`.  
PhaseOutcome: `InProgress`, `Completed`, `Skipped`, `Interrupted`.  
ContinueChoice: `Continue`, `StartFresh`.

PhaseLog: `Id`, `Phase`, `Cycle`, `StartedAt`, `EndedAt`, `PlannedDuration`, `Elapsed`, `Outcome`.  
SessionSnapshot: `Phase`, `Cycle`, `Remaining`, `TotalPhaseDuration`, `IsRunning`, `IsPaused`, `UpdatedAt`.

PhaseTransition: add optional `PhaseEndReason reason = PhaseEndReason.Completed` so existing `new PhaseTransition(a, b, c)` still compiles. Expose `Reason`.

```csharp
namespace FocusPomodoro.Helpers;

public static class PhaseElapsed
{
    public static TimeSpan FromRemaining(TimeSpan planned, TimeSpan remaining)
    {
        if (planned <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var elapsed = planned - remaining;
        if (elapsed < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return elapsed > planned ? planned : elapsed;
    }
}
```

- [ ] **Step 4: Run PhaseElapsedTests — expect PASS**

---

### Task 2: DailyHistory and HistoryPanelLayout

**Files:**
- Create: `FocusPomodoro/Helpers/DailyHistory.cs`
- Create: `FocusPomodoro/Helpers/HistoryPanelLayout.cs`
- Test: `FocusPomodoro.Tests/DailyHistoryTests.cs`
- Test: `FocusPomodoro.Tests/HistoryPanelLayoutTests.cs`

- [ ] **Step 1: Failing tests**

DailyHistory groups by **local** date of `StartedAt` (convert UTC → `timeZone`), newest day first. Header counts only `Phase == Focus && Outcome == Completed`. Sum those `Elapsed`.

HistoryPanelLayout: `GapPixels = 8`, `DefaultWidthDips = 280`, `DefaultHeightDips = 400`.  
`Place(PixelRect workArea, PixelRect owner, PixelSize panel, int gap = GapPixels)` → `PixelPoint`. Prefer `owner.X + owner.Width + gap`; if `right + panel.Width > workArea.X + workArea.Width`, use `owner.X - panel.Width - gap`. Clamp X/Y into work area (same clamp idea as `WindowLayout.BottomRight`).

- [ ] **Step 2: Run tests — expect fail**

- [ ] **Step 3: Implement helpers + link in test csproj**

- [ ] **Step 4: Run tests — expect PASS**

---

### Task 3: Timer Checkpoint, Restore, Interrupted transitions

**Files:**
- Modify: `FocusPomodoro/Services/IPomodoroTimerService.cs`
- Modify: `FocusPomodoro/Services/PomodoroTimerService.cs`
- Modify: `FocusPomodoro.Tests/PomodoroTimerServiceTests.cs`
- Modify: `FocusPomodoro.Tests/SettingsViewModelTests.cs` (fake: add `Checkpoint` + `Restore`)

- [ ] **Step 1: Add failing tests to PomodoroTimerServiceTests**

1. `CompleteCurrentPhase_RaisesPhaseTransitionedWithCompletedReason`
2. `SkipToNextPhase_RaisesPhaseTransitionedWithSkippedReason`
3. `RestartCurrentPhase_WhenRunning_RaisesInterruptedAndKeepsRunning`
4. `ResetCycle_WhenRunning_RaisesInterrupted` — **replace** `ResetCycle_DoesNotRaisePhaseTransitioned` and the `Assert.Empty(events.PhaseTransitions)` in `ResetCycle_WhenAlreadyFocus_DoesNotRaisePhaseChanged` (reset from active focus **does** interrupt). Idle reset still does not raise `PhaseTransitioned`.
5. `Start_RaisesCheckpoint_TickDoesNot`
6. `Pause_AndResume_RaiseCheckpoint`
7. `Restore_WhenWasRunning_ResumesFromRemainingWithoutCountingClosedTime`

Restore test: Start, advance 10s, tick, capture remaining, `Restore` a session with that remaining, `IsRunning = true`, after `time.Advance(30s)` **without** restore the old EndTime would have been wrong — restore must set `EndTime = now + remaining` at restore time.

```csharp
[Fact]
public void Restore_WhenWasRunning_SetsEndTimeFromNowPlusRemaining()
{
    var (service, time, ticker) = CreateSut();
    var snapshot = new PomodoroSession
    {
        CurrentPhase = PomodoroPhase.Focus,
        CurrentCycle = 2,
        IsRunning = true,
        IsPaused = false,
        RemainingTime = TimeSpan.FromMinutes(12),
        TotalPhaseDuration = TimeSpan.FromMinutes(25)
    };

    time.Advance(TimeSpan.FromMinutes(5));
    service.Restore(snapshot);

    var state = service.GetState();
    Assert.Equal(PomodoroPhase.Focus, state.CurrentPhase);
    Assert.Equal(2, state.CurrentCycle);
    Assert.True(state.IsRunning);
    Assert.Equal(TimeSpan.FromMinutes(12), state.RemainingTime);
    Assert.Equal(time.GetUtcNow() + TimeSpan.FromMinutes(12), state.EndTime);
    Assert.True(ticker.IsStarted);
}
```

- [ ] **Step 2: Run those tests — expect fail**

- [ ] **Step 3: Implement**

`IPomodoroTimerService`:
```csharp
event EventHandler? Checkpoint;
void Restore(PomodoroSession state);
```

`RaiseCheckpoint()` on Start (from idle), Pause, Resume, Restart, ResetCycle, Skip, Complete — **not** on tick.

`RaisePhaseTransitioned(completed, reason)` — Complete → `Completed`, Skip → `Skipped`, Restart when `IsRunning || IsPaused` → `Interrupted` (same phase as next), ResetCycle when `IsRunning || IsPaused` → `Interrupted` (next is Focus after reset). Idle restart/reset: no `PhaseTransitioned`.

`Restore`: copy fields; if `IsRunning`, `BeginRunning()` with the restored remaining; if paused, freeze idle ticker; if idle, `GoIdle` with remaining/duration applied.

- [ ] **Step 4: Full timer tests PASS**, including existing skip/complete tests (Reason defaults to Completed for catalog tests).

---

### Task 4: Ignore Interrupted in sound and toast

**Files:**
- Modify: `FocusPomodoro/Services/NotificationService.cs`
- Modify: `FocusPomodoro/Services/SoundService.cs`
- Test: `FocusPomodoro.Tests/NotificationServiceTests.cs`
- Test: `FocusPomodoro.Tests/SoundServiceTests.cs`

- [ ] **Step 1: Tests** `ResetCycle_WhenRunning_DoesNotShowToast` / `RestartCurrentPhase_WhenRunning_DoesNotPlaySound` (after Task 3 these would fail if unfiltered). Sound already has `ResetCycle_DoesNotPlaySound` — keep it; it must still pass.

- [ ] **Step 2: Filter** `if (transition.Reason == PhaseEndReason.Interrupted) return;`

- [ ] **Step 3: Tests PASS**

---

### Task 5: SqliteHistoryStore

**Files:**
- Package `Microsoft.Data.Sqlite` Version `9.0.9` on both csproj
- Create: `FocusPomodoro/Services/IHistoryStore.cs`
- Create: `FocusPomodoro/Services/SqliteHistoryStore.cs`
- Test: `FocusPomodoro.Tests/SqliteHistoryStoreTests.cs`

- [ ] **Step 1: Failing tests against IHistoryStore using a temp `pomodoro.db`**

- `Initialize_CreatesSchemaAndUserVersion1`
- `OpenAndClose_RoundTripsCompletedLog`
- `SaveAndLoadSnapshot_RoundTrips`
- `GetInProgress_ReturnsOpenRow`
- `Initialize_WhenMultipleInProgress_KeepsLatestInterruptsOlder`
- `Initialize_WhenFileIsGarbage_RenamesBakAndCreatesFresh` (write `not a sqlite file` bytes, initialize, assert `.bak` exists and store works)

Interface:

```csharp
public interface IHistoryStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<PhaseLog> OpenPhaseAsync(PomodoroPhase phase, int cycle, TimeSpan planned, DateTimeOffset startedAt, CancellationToken cancellationToken = default);
    Task ClosePhaseAsync(long id, DateTimeOffset endedAt, TimeSpan elapsed, PhaseOutcome outcome, CancellationToken cancellationToken = default);
    Task<PhaseLog?> GetInProgressAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhaseLog>> GetLogsAsync(CancellationToken cancellationToken = default);
    Task SaveSnapshotAsync(SessionSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<SessionSnapshot?> LoadSnapshotAsync(CancellationToken cancellationToken = default);
}
```

`SqliteHistoryStore(string filePath)`. `FileName = "pomodoro.db"`. WAL optional. Catch corrupt open → move to `pomodoro.db.bak` (delete existing bak first) → create new.

SQL times as `o` round-trip. Enums as `ToString()`.

- [ ] **Step 2: Run — fail**

- [ ] **Step 3: Implement store + link files**

- [ ] **Step 4: Tests PASS**

---

### Task 6: SessionHistoryService

**Files:**
- Create: `FocusPomodoro/Services/ISessionHistoryService.cs`
- Create: `FocusPomodoro/Services/SessionHistoryService.cs`
- Create: `FocusPomodoro.Tests/FakeHistoryStore.cs`
- Test: `FocusPomodoro.Tests/SessionHistoryServiceTests.cs`

- [ ] **Step 1: Tests with real `PomodoroTimerService` + `FakeHistoryStore` + `FakeTimeProvider`**

- Start opens InProgress + snapshot
- Tick for 1s does not write extra snapshots (or at most the start one)
- Complete closes Completed; auto-start opens next
- Skip closes Skipped
- Restart while running: Interrupted then new InProgress same phase
- Reset while running: Interrupted, no new InProgress, idle snapshot
- `PersistAsync` while running keeps InProgress, snapshot remaining frozen from EndTime
- `TryGetResumable` true when snapshot running/paused and remaining > 0 and InProgress exists
- `StartFreshAsync` closes Interrupted with elapsed from remaining, then caller resets timer (service method should close log + save idle snapshot; timer `ResetCycle` is called by VM)
- After Start, advance 16s of fake time with ticks: second snapshot save (15s throttle)

FakeHistoryStore: in-memory lists, thread-safe enough for tests.

`ISessionHistoryService`:
```csharp
void Attach();
Task InitializeAsync(CancellationToken cancellationToken = default);
Task PersistAsync(CancellationToken cancellationToken = default);
bool TryGetResumable(out SessionSnapshot snapshot);
Task StartFreshAsync(CancellationToken cancellationToken = default);
```

On `StateChanged`: if running and `now - lastSnapshot >= 15s`, save snapshot (compute remaining from `EndTime`). Swallow IO exceptions from store.

Invalid snapshot (negative remaining): `TryGetResumable` returns false.

- [ ] **Step 2–4: TDD implement, tests PASS**

---

### Task 7: History presentation + HistoryViewModel

**Files:**
- Create: `FocusPomodoro/Helpers/HistoryPresentation.cs`
- Create: `FocusPomodoro/ViewModels/HistoryViewModel.cs`
- Test: `FocusPomodoro.Tests/HistoryPresentationTests.cs`

- [ ] **Step 1: Presentation tests** — outcome labels: `Em andamento`, `Concluída`, `Pulada`, `Interrompida`. Phase names reuse `PomodoroPresentation.CurrentPhaseText`. Duration like `TimeRemainingText`. Line: `{time}  {phase}  {duration}  {outcome}`. Continue prompt: `$"Continuar {PomodoroPresentation.TimeRemainingText(remaining)} restantes?"`

- [ ] **Step 2: HistoryViewModel.LoadAsync** reads store, sets `Days` from `DailyHistory.GroupByLocalDay`. Empty store → empty list, no throw.

- [ ] **Step 3: Tests PASS** (VM test: FakeHistoryStore with two logs on different local days)

---

### Task 8: WinUI wiring

**Files:**
- Create ContinueChoiceWindow (clone CloseChoiceWindow: two buttons Continuar / Começar de novo; set message from ctor or property)
- Create HistoryPanelWindow (280×400, list bound to HistoryViewModel)
- Modify MainViewModel: history toggle command, ContinueChoiceRequested, persist on ExitCoreAsync, open/close panel events
- Modify MainWindow: history icon, position panel via HistoryPanelLayout + AppWindow, follow owner Changed
- Modify App.xaml.cs: register store path `pomodoro.db`, InitializeAsync + Attach history, after Activate if TryGetResumable then dialog

Exit: `await history.PersistAsync()` inside `ExitCoreAsync` before close, ignore exceptions.

Resume after `_window.Activate()`:
```
if (history.TryGetResumable(out var snap))
{
    var choice = await vm.RequestContinueAsync();
    if (choice == ContinueChoice.Continue)
        timer.Restore(SessionFrom(snap));
    else
    {
        await history.StartFreshAsync();
        timer.ResetCycle();
    }
}
```

Map snapshot → PomodoroSession in a small helper `SessionSnapshot.ToSession()`.

- [ ] **Step 1: Implement UI (no WinUI unit tests; layout already tested)**

- [ ] **Step 2: `dotnet test FocusPomodoro.Tests/FocusPomodoro.Tests.csproj` all green**

- [ ] **Step 3: `dotnet build FocusPomodoro/FocusPomodoro.csproj` succeeds**

---

## Self-review vs spec

| Spec | Task |
| --- | --- |
| phase_logs + session_state SQLite | 5 |
| Recording rules / 15s throttle / Exit freeze | 6 |
| Continue vs Start fresh | 6 + 8 |
| Checkpoint / Restore / reasons | 3 |
| Sound/toast ignore Interrupted | 4 |
| Daily group + panel place | 2, 7, 8 |
| Corrupt db → .bak | 5 |
| Hide to tray unchanged | 8 (no persist on hide) |
| `total_duration_ms` (needed for Restore) | 5 + spec note |
