using FocusPomodoro.Models;
using FocusPomodoro.Services;
using FocusPomodoro.ViewModels;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void Constructor_CopiesCurrentSettingsIntoEditableProperties()
    {
        var settings = new PomodoroSettings
        {
            FocusDurationMinutes = 40,
            ShortBreakDurationMinutes = 7,
            LongBreakDurationMinutes = 18,
            CyclesBeforeLongBreak = 5,
            AutoStartNextPhase = false,
            AlwaysOnTop = true,
            SoundEnabled = false,
            NotificationsEnabled = false,
            AppTheme = AppTheme.Light,
            MinimizeToTrayOnClose = true
        };
        var settingsService = new FakeSettingsService(settings);

        var viewModel = new SettingsViewModel(settingsService, new FakePomodoroTimerService());

        Assert.Equal(40, viewModel.FocusDurationMinutes);
        Assert.Equal(7, viewModel.ShortBreakDurationMinutes);
        Assert.Equal(18, viewModel.LongBreakDurationMinutes);
        Assert.Equal(5, viewModel.CyclesBeforeLongBreak);
        Assert.False(viewModel.AutoStartNextPhase);
        Assert.True(viewModel.AlwaysOnTop);
        Assert.False(viewModel.SoundEnabled);
        Assert.False(viewModel.NotificationsEnabled);
        Assert.Equal(AppTheme.Light, viewModel.AppTheme);
        Assert.True(viewModel.MinimizeToTrayOnClose);
    }

    [Fact]
    public async Task Save_PersistsEditsAppliesTimerAndRequestsClose()
    {
        var settings = new PomodoroSettings();
        var settingsService = new FakeSettingsService(settings);
        var timer = new FakePomodoroTimerService();
        var viewModel = new SettingsViewModel(settingsService, timer);
        var closed = 0;
        viewModel.CloseRequested += (_, _) => closed++;

        viewModel.FocusDurationMinutes = 45;
        viewModel.ShortBreakDurationMinutes = 10;
        viewModel.LongBreakDurationMinutes = 20;
        viewModel.CyclesBeforeLongBreak = 6;
        viewModel.AutoStartNextPhase = false;
        viewModel.AlwaysOnTop = true;
        viewModel.SoundEnabled = false;
        viewModel.NotificationsEnabled = false;
        viewModel.AppTheme = AppTheme.Light;
        viewModel.MinimizeToTrayOnClose = true;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(1, settingsService.SaveCount);
        Assert.Equal(45, settings.FocusDurationMinutes);
        Assert.Equal(10, settings.ShortBreakDurationMinutes);
        Assert.Equal(20, settings.LongBreakDurationMinutes);
        Assert.Equal(6, settings.CyclesBeforeLongBreak);
        Assert.False(settings.AutoStartNextPhase);
        Assert.True(settings.AlwaysOnTop);
        Assert.False(settings.SoundEnabled);
        Assert.False(settings.NotificationsEnabled);
        Assert.Equal(AppTheme.Light, settings.AppTheme);
        Assert.True(settings.MinimizeToTrayOnClose);
        Assert.Equal(-1, settings.WindowPositionX);
        Assert.Equal(260, settings.WindowWidth);
        Assert.Equal(1, timer.ApplySettingsCount);
        Assert.Equal(1, closed);
    }

    [Fact]
    public void Cancel_DoesNotPersistAndRequestsClose()
    {
        var settings = new PomodoroSettings();
        var settingsService = new FakeSettingsService(settings);
        var viewModel = new SettingsViewModel(settingsService, new FakePomodoroTimerService());
        var closed = 0;
        viewModel.CloseRequested += (_, _) => closed++;

        viewModel.FocusDurationMinutes = 45;
        viewModel.CancelCommand.Execute(null);

        Assert.Equal(0, settingsService.SaveCount);
        Assert.Equal(25, settings.FocusDurationMinutes);
        Assert.Equal(1, closed);
    }

    [Fact]
    public void RestoreDefaults_ResetsEditablePropertiesWithoutSaving()
    {
        var settings = new PomodoroSettings
        {
            FocusDurationMinutes = 40,
            AppTheme = AppTheme.Light,
            AlwaysOnTop = true
        };
        var settingsService = new FakeSettingsService(settings);
        var viewModel = new SettingsViewModel(settingsService, new FakePomodoroTimerService());

        viewModel.RestoreDefaultsCommand.Execute(null);

        Assert.Equal(25, viewModel.FocusDurationMinutes);
        Assert.Equal(5, viewModel.ShortBreakDurationMinutes);
        Assert.Equal(AppTheme.Dark, viewModel.AppTheme);
        Assert.False(viewModel.AlwaysOnTop);
        Assert.Equal(0, settingsService.SaveCount);
        Assert.Equal(40, settings.FocusDurationMinutes);
    }

    private sealed class FakeSettingsService : ISettingsService
    {
        public FakeSettingsService(PomodoroSettings current) => Current = current;

        public PomodoroSettings Current { get; }
        public int SaveCount { get; private set; }
        public event EventHandler? SettingsChanged;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SaveAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePomodoroTimerService : IPomodoroTimerService
    {
        public int ApplySettingsCount { get; private set; }

        public event EventHandler? StateChanged
        {
            add { }
            remove { }
        }

        public event EventHandler<PomodoroPhase>? PhaseChanged
        {
            add { }
            remove { }
        }

        public event EventHandler<PhaseTransition>? PhaseTransitioned
        {
            add { }
            remove { }
        }

        public event EventHandler? Checkpoint
        {
            add { }
            remove { }
        }

        public PomodoroSession GetState() => new();
        public void Start() { }
        public void Pause() { }
        public void Resume() { }
        public void RestartCurrentPhase() { }
        public void ResetCycle() { }
        public void SkipToNextPhase() { }
        public void ApplySettings() => ApplySettingsCount++;
        public void Restore(PomodoroSession state) { }
    }
}
