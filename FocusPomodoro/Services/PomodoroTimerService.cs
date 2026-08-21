using FocusPomodoro.Models;

namespace FocusPomodoro.Services;

public sealed class PomodoroTimerService : IPomodoroTimerService
{
    private readonly PomodoroSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly IUiTicker _ticker;
    private readonly PomodoroSession _session;

    public event EventHandler? StateChanged;
    public event EventHandler<PomodoroPhase>? PhaseChanged;
    public event EventHandler<PhaseTransition>? PhaseTransitioned;

    public PomodoroTimerService(
        PomodoroSettings settings,
        TimeProvider timeProvider,
        IUiTicker ticker)
    {
        _settings = settings;
        _timeProvider = timeProvider;
        _ticker = ticker;
        _session = new PomodoroSession();
        ApplyPhaseDuration(_session.CurrentPhase);
        _ticker.Tick += OnTick;
    }

    public PomodoroSession GetState() => new()
    {
        CurrentPhase = _session.CurrentPhase,
        CurrentCycle = _session.CurrentCycle,
        IsRunning = _session.IsRunning,
        IsPaused = _session.IsPaused,
        RemainingTime = _session.RemainingTime,
        TotalPhaseDuration = _session.TotalPhaseDuration,
        EndTime = _session.EndTime
    };

    public void Start()
    {
        if (_session.IsRunning)
        {
            RaiseStateChanged();
            return;
        }

        if (_session.IsPaused)
        {
            Resume();
            return;
        }

        BeginRunning();
        RaiseStateChanged();
    }

    public void Pause()
    {
        if (!_session.IsRunning)
        {
            RaiseStateChanged();
            return;
        }

        FreezeRemaining();
        _session.IsRunning = false;
        _session.IsPaused = true;
        _session.EndTime = null;
        _ticker.Stop();

        if (_session.RemainingTime <= TimeSpan.Zero)
        {
            CompleteCurrentPhase();
            return;
        }

        RaiseStateChanged();
    }

    public void Resume()
    {
        if (!_session.IsPaused)
        {
            RaiseStateChanged();
            return;
        }

        BeginRunning();
        RaiseStateChanged();
    }

    public void RestartCurrentPhase()
    {
        var duration = DurationFor(_session.CurrentPhase);
        _session.RemainingTime = duration;
        _session.TotalPhaseDuration = duration;
        _session.EndTime = _session.IsRunning
            ? _timeProvider.GetUtcNow() + duration
            : null;

        RaiseStateChanged();
    }

    public void ResetCycle()
    {
        var previousPhase = _session.CurrentPhase;
        GoIdle();
        _session.CurrentPhase = PomodoroPhase.Focus;
        _session.CurrentCycle = 1;
        ApplyPhaseDuration(PomodoroPhase.Focus);

        if (previousPhase != PomodoroPhase.Focus)
        {
            RaisePhaseChanged();
        }

        RaiseStateChanged();
    }

    public void SkipToNextPhase()
    {
        var completed = _session.CurrentPhase;
        AdvanceToNextPhase();
        StartOrIdleAfterPhaseChange();
        RaisePhaseTransitioned(completed);
        RaisePhaseChanged();
        RaiseStateChanged();
    }

    public void ApplySettings()
    {
        var newDuration = DurationFor(_session.CurrentPhase);
        if (newDuration != _session.TotalPhaseDuration)
        {
            if (_session.IsRunning || _session.IsPaused)
            {
                GoIdle();
            }

            ApplyPhaseDuration(_session.CurrentPhase);
        }

        RaiseStateChanged();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (!_session.IsRunning || _session.EndTime is null)
        {
            return;
        }

        RecalculateRemaining();
        if (_session.RemainingTime <= TimeSpan.Zero)
        {
            CompleteCurrentPhase();
            return;
        }

        RaiseStateChanged();
    }

    private void CompleteCurrentPhase()
    {
        var completed = _session.CurrentPhase;
        AdvanceToNextPhase();
        StartOrIdleAfterPhaseChange();
        RaisePhaseTransitioned(completed);
        RaisePhaseChanged();
        RaiseStateChanged();
    }

    private void StartOrIdleAfterPhaseChange()
    {
        if (_settings.AutoStartNextPhase)
        {
            BeginRunning();
        }
        else
        {
            GoIdle();
        }
    }

    private void BeginRunning()
    {
        _session.IsRunning = true;
        _session.IsPaused = false;
        _session.EndTime = _timeProvider.GetUtcNow() + _session.RemainingTime;
        _ticker.Start();
    }

    private void GoIdle()
    {
        _session.IsRunning = false;
        _session.IsPaused = false;
        _session.EndTime = null;
        _ticker.Stop();
    }

    private void FreezeRemaining()
    {
        if (_session.EndTime is not { } end)
        {
            return;
        }

        var remaining = end - _timeProvider.GetUtcNow();
        _session.RemainingTime = remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    private void RecalculateRemaining()
    {
        var remaining = _session.EndTime!.Value - _timeProvider.GetUtcNow();
        _session.RemainingTime = remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    private void AdvanceToNextPhase()
    {
        switch (_session.CurrentPhase)
        {
            case PomodoroPhase.Focus:
                _session.CurrentPhase = _session.CurrentCycle < _settings.CyclesBeforeLongBreak
                    ? PomodoroPhase.ShortBreak
                    : PomodoroPhase.LongBreak;
                break;
            case PomodoroPhase.ShortBreak:
                _session.CurrentPhase = PomodoroPhase.Focus;
                _session.CurrentCycle++;
                break;
            case PomodoroPhase.LongBreak:
                _session.CurrentPhase = PomodoroPhase.Focus;
                _session.CurrentCycle = 1;
                break;
        }

        ApplyPhaseDuration(_session.CurrentPhase);
    }

    private void ApplyPhaseDuration(PomodoroPhase phase)
    {
        var duration = DurationFor(phase);
        _session.RemainingTime = duration;
        _session.TotalPhaseDuration = duration;
    }

    private TimeSpan DurationFor(PomodoroPhase phase) => phase switch
    {
        PomodoroPhase.Focus => TimeSpan.FromMinutes(_settings.FocusDurationMinutes),
        PomodoroPhase.ShortBreak => TimeSpan.FromMinutes(_settings.ShortBreakDurationMinutes),
        PomodoroPhase.LongBreak => TimeSpan.FromMinutes(_settings.LongBreakDurationMinutes),
        _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
    };

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private void RaisePhaseChanged() => PhaseChanged?.Invoke(this, _session.CurrentPhase);

    private void RaisePhaseTransitioned(PomodoroPhase completed) =>
        PhaseTransitioned?.Invoke(
            this,
            new PhaseTransition(completed, _session.CurrentPhase, _session.CurrentCycle));
}
