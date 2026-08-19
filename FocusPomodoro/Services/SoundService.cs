using FocusPomodoro.Models;

namespace FocusPomodoro.Services;

public sealed class SoundService : ISoundService
{
    private readonly IPomodoroTimerService _timer;
    private readonly PomodoroSettings _settings;
    private readonly Action _playSound;
    private bool _attached;

    public SoundService(
        IPomodoroTimerService timer,
        PomodoroSettings settings,
        Action playSound)
    {
        _timer = timer ?? throw new ArgumentNullException(nameof(timer));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _playSound = playSound ?? throw new ArgumentNullException(nameof(playSound));
        Attach();
    }

    public void Attach()
    {
        if (_attached)
        {
            return;
        }

        _attached = true;
        _timer.PhaseTransitioned += OnPhaseTransitioned;
    }

    public void PlayPhaseChange()
    {
        if (!_settings.SoundEnabled)
        {
            return;
        }

        _playSound();
    }

    private void OnPhaseTransitioned(object? sender, PhaseTransition transition) =>
        PlayPhaseChange();
}
