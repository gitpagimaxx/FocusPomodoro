# Pomodoro Timer Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the core Pomodoro timer logic and wire it to the existing WinUI 3 MVVM UI so the user can start, pause, resume, restart, and skip phases with accurate remaining time.

**Architecture:** Domain models hold session state. `PomodoroTimerService` uses `DateTimeOffset` end timestamps (`TimeProvider.GetUtcNow()`) as the source of truth. A `DispatcherQueueTimer` (via `IUiTicker`) refreshes the UI about once per second by recomputing `EndTime - now`. `MainViewModel` subscribes to service events and exposes commands/properties. No persistence, tray, notifications, or window chrome changes.

**Tech Stack:** WinUI 3, .NET 10, CommunityToolkit.Mvvm 8.4, Microsoft.Extensions.DependencyInjection, xUnit for core tests.

---

## File structure

- Create: `FocusPomodoro/Models/PomodoroPhase.cs`
- Create: `FocusPomodoro/Models/PomodoroSettings.cs`
- Create: `FocusPomodoro/Models/PomodoroSession.cs`
- Create: `FocusPomodoro/Services/IUiTicker.cs`
- Create: `FocusPomodoro/Services/DispatcherQueueTicker.cs`
- Create: `FocusPomodoro/Services/IPomodoroTimerService.cs`
- Create: `FocusPomodoro/Services/PomodoroTimerService.cs`
- Create: `FocusPomodoro.Tests/FocusPomodoro.Tests.csproj`
- Create: `FocusPomodoro.Tests/FakeUiTicker.cs`
- Create: `FocusPomodoro.Tests/PomodoroTimerServiceTests.cs`
- Modify: `FocusPomodoro/ViewModels/MainViewModel.cs`
- Modify: `FocusPomodoro/MainWindow.xaml`
- Modify: `FocusPomodoro/App.xaml.cs`

**Out of scope:** persistence, system tray, toast notifications, window customization, settings UI.

**Do not commit.** This workspace is not a git repository. Do not run `git init` or `git commit`.

---

### Task 1: Domain models

**Files:**
- Create: `FocusPomodoro/Models/PomodoroPhase.cs`
- Create: `FocusPomodoro/Models/PomodoroSettings.cs`
- Create: `FocusPomodoro/Models/PomodoroSession.cs`

- [ ] **Step 1: Create `PomodoroPhase`**

```csharp
namespace FocusPomodoro.Models;

public enum PomodoroPhase
{
    Focus,
    ShortBreak,
    LongBreak
}
```

- [ ] **Step 2: Create `PomodoroSettings` with these exact default values**

```csharp
namespace FocusPomodoro.Models;

public sealed class PomodoroSettings
{
    public int FocusDurationMinutes { get; set; } = 25;
    public int ShortBreakDurationMinutes { get; set; } = 5;
    public int LongBreakDurationMinutes { get; set; } = 15;
    public int CyclesBeforeLongBreak { get; set; } = 4;
    public bool AutoStartNextPhase { get; set; } = true;
}
```

- [ ] **Step 3: Create `PomodoroSession`**

```csharp
namespace FocusPomodoro.Models;

public sealed class PomodoroSession
{
    public PomodoroPhase CurrentPhase { get; set; } = PomodoroPhase.Focus;
    public int CurrentCycle { get; set; } = 1;
    public bool IsRunning { get; set; }
    public bool IsPaused { get; set; }
    public TimeSpan RemainingTime { get; set; }
    public TimeSpan TotalPhaseDuration { get; set; }
    public DateTimeOffset? EndTime { get; set; }
}
```

- [ ] **Step 4: Do not commit**

---

### Task 2: Timer service (TDD)

**Files:**
- Create: `FocusPomodoro/Services/IUiTicker.cs`
- Create: `FocusPomodoro/Services/IPomodoroTimerService.cs`
- Create: `FocusPomodoro/Services/PomodoroTimerService.cs`
- Create: `FocusPomodoro/Services/DispatcherQueueTicker.cs`
- Create: `FocusPomodoro.Tests/FocusPomodoro.Tests.csproj`
- Create: `FocusPomodoro.Tests/FakeUiTicker.cs`
- Create: `FocusPomodoro.Tests/PomodoroTimerServiceTests.cs`

**Source of truth:** `EndTime` (`DateTimeOffset`). Never implement the countdown as `RemainingTime -= 1 second` on each tick. On each tick: `RemainingTime = max(EndTime - now, 0)`.

**Phase flow:**
1. Focus (cycle N)
2. If `CurrentCycle < CyclesBeforeLongBreak` → ShortBreak (same cycle)
3. After ShortBreak → Focus with `CurrentCycle + 1`
4. After Focus when `CurrentCycle == CyclesBeforeLongBreak` → LongBreak (same cycle)
5. After LongBreak → Focus with `CurrentCycle = 1`

Start state before `Start()`: Focus, cycle 1, not running, not paused, remaining = focus duration, `EndTime = null`.

**Methods:**
- `Start()` — if already running, no-op. If paused, behave like Resume. Otherwise start current phase: `IsRunning=true`, `IsPaused=false`, `EndTime = now + RemainingTime`, start ticker.
- `Pause()` — if not running, no-op. Freeze remaining: `RemainingTime = max(EndTime - now, 0)`, `EndTime = null`, `IsRunning=false`, `IsPaused=true`, stop ticker.
- `Resume()` — if not paused, no-op. `EndTime = now + RemainingTime`, `IsRunning=true`, `IsPaused=false`, start ticker.
- `RestartCurrentPhase()` — reset remaining/total to the full duration of the current phase. If running, `EndTime = now + duration`. If paused, stay paused with full remaining and `EndTime = null`. If idle, stay idle with full remaining.
- `SkipToNextPhase()` — advance phase using the flow above. Reset remaining to the new phase duration. If `AutoStartNextPhase`, start the new phase (running). Otherwise idle on the new phase.
- `GetState()` — return a **copy** of the current session (do not expose a mutable live instance).

**When remaining reaches 0 (tick or command):** complete the current phase, raise `PhaseChanged`, apply the same next-phase rules as Skip. If `AutoStartNextPhase`, start next; else idle.

**Events:**
- `event EventHandler? StateChanged` — after every command and every tick
- `event EventHandler<PomodoroPhase>? PhaseChanged` — when the phase actually changes (sender + new phase)

**Clock:** inject `TimeProvider`. Use `_timeProvider.GetUtcNow()`.

**UI ticker:** inject `IUiTicker`. Do not reference `Microsoft.UI.*` inside `PomodoroTimerService`.

```csharp
namespace FocusPomodoro.Services;

public interface IUiTicker
{
    event EventHandler? Tick;
    void Start();
    void Stop();
}
```

```csharp
namespace FocusPomodoro.Services;

public interface IPomodoroTimerService
{
    event EventHandler? StateChanged;
    event EventHandler<PomodoroPhase>? PhaseChanged;

    PomodoroSession GetState();
    void Start();
    void Pause();
    void Resume();
    void RestartCurrentPhase();
    void SkipToNextPhase();
}
```

`DispatcherQueueTicker` wraps `DispatcherQueue.GetForCurrentThread().CreateTimer()` with `Interval = TimeSpan.FromSeconds(1)`, invokes `Tick` on each timer tick, `Start()`/`Stop()` map to `IsEnabled`.

**Tests (TDD — fail first):** use `Microsoft.Extensions.Time.Testing.FakeTimeProvider` and `FakeUiTicker` that exposes `RaiseTick()`. Test project is `net10.0` and **compiles the core source files by link** (not a WinUI project reference):

Link these files only:
- `FocusPomodoro/Models/PomodoroPhase.cs`
- `FocusPomodoro/Models/PomodoroSettings.cs`
- `FocusPomodoro/Models/PomodoroSession.cs`
- `FocusPomodoro/Services/IUiTicker.cs`
- `FocusPomodoro/Services/IPomodoroTimerService.cs`
- `FocusPomodoro/Services/PomodoroTimerService.cs`

Do **not** link `DispatcherQueueTicker.cs`.

Required test behaviors:
1. Initial state is Focus, cycle 1, 25:00, not running.
2. Start sets `EndTime = now + 25 minutes` and `IsRunning`.
3. After advancing time 10s and tick, remaining is ~24:50 (not 24:59 from a decrementer). Use `FakeTimeProvider.Advance`.
4. Pause freezes remaining; further time advance + tick does not reduce remaining.
5. Resume sets `EndTime = now + frozen remaining`.
6. After 1 completed Focus, phase is ShortBreak, cycle still 1.
7. After Focus 4 completes, phase is LongBreak.
8. After LongBreak completes, phase is Focus, cycle 1.
9. SkipToNextPhase from Focus 1 goes to ShortBreak.
10. RestartCurrentPhase restores full duration of the current phase.
11. `GetState()` returns a copy (mutating the returned object does not change the service).

Run: `dotnet test FocusPomodoro.Tests/FocusPomodoro.Tests.csproj`

- [ ] **Step 1: Create test project + FakeUiTicker + first failing tests**
- [ ] **Step 2: Run tests — they must fail for missing types**
- [ ] **Step 3: Implement interfaces + service + ticker (minimal)**
- [ ] **Step 4: Run tests until all pass**
- [ ] **Step 5: Do not commit**

---

### Task 3: ViewModel, XAML, DI

**Files:**
- Modify: `FocusPomodoro/ViewModels/MainViewModel.cs`
- Modify: `FocusPomodoro/MainWindow.xaml`
- Modify: `FocusPomodoro/App.xaml.cs`

Keep CommunityToolkit.Mvvm `partial` observable properties (same style as existing `MainViewModel`).

**MainViewModel** injects `IPomodoroTimerService`. Subscribe to `StateChanged` and `PhaseChanged`. On each state update:

- `PhaseName`: Focus → `"Foco"`; ShortBreak → `"Pausa curta"`; LongBreak → `"Pausa longa"`
- `TimeDisplay`: `mm:ss` from `RemainingTime` using total minutes and seconds, e.g. `$"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}"`
- `CycleText`: `$"Ciclo {CurrentCycle} de {settings.CyclesBeforeLongBreak}"` (use 4 from settings)
- Button flags:
  - `IsStartEnabled` = `!IsRunning && !IsPaused`
  - `IsPauseEnabled` = `IsRunning`
  - `IsResumeEnabled` = `IsPaused`
  - `IsRestartEnabled` = `IsRunning || IsPaused`
  - `IsSkipEnabled` = `IsRunning || IsPaused`

Commands: `Start`, `Pause`, `Resume`, `Restart` (calls `RestartCurrentPhase`), `Skip` (calls `SkipToNextPhase`). Each uses `CanExecute` bound to the matching flag, and `NotifyCanExecuteChanged` when the flag changes (same pattern as existing `Start`).

Remove unused `StartButtonText` or keep it as `"Iniciar"` if still bound. Prefer five labeled buttons.

**MainWindow.xaml:** keep title bar and existing typography/colors. Replace the single Start button with five buttons (Iniciar, Pausar, Retomar, Reiniciar, Pular). Primary (Iniciar) keeps the accent style; others can be default. Bind `Command` and `IsEnabled` to the ViewModel. Do not change window size, title bar, or backdrop.

**App.xaml.cs DI:**

```csharp
services.AddSingleton(TimeProvider.System);
services.AddSingleton<PomodoroSettings>();
services.AddSingleton<IUiTicker, DispatcherQueueTicker>();
services.AddSingleton<IPomodoroTimerService, PomodoroTimerService>();
services.AddTransient<MainViewModel>();
services.AddTransient<MainWindow>();
```

`PomodoroTimerService` constructor: `(PomodoroSettings settings, TimeProvider timeProvider, IUiTicker ticker)`.

Apply initial state to the ViewModel in the constructor (so the UI shows 25:00 / Foco / Ciclo 1 de 4 before Start).

- [ ] **Step 1: Update ViewModel**
- [ ] **Step 2: Update XAML buttons**
- [ ] **Step 3: Register DI**
- [ ] **Step 4: `dotnet build FocusPomodoro/FocusPomodoro.csproj -c Debug` must succeed**
- [ ] **Step 5: Do not commit**

---

### Task 4: Verify compile and architecture

- [ ] Run `dotnet test FocusPomodoro.Tests/FocusPomodoro.Tests.csproj`
- [ ] Run `dotnet build FocusPomodoro/FocusPomodoro.csproj -c Debug`
- [ ] Confirm MVVM: View has no timer logic; ViewModel has no `DispatcherQueueTimer` / end-time math; service owns timer rules
- [ ] Confirm no persistence, tray, notifications, or window customization were added
