using FocusPomodoro.Models;
using FocusPomodoro.Services;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory().FullName;

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_CreatesFileWithDefaults()
    {
        var settings = new PomodoroSettings();
        var path = FilePath();
        var service = new SettingsService(settings, path);

        await service.LoadAsync();

        Assert.True(File.Exists(path));
        AssertDefaultValues(settings);

        var reloaded = new PomodoroSettings();
        await new SettingsService(reloaded, path).LoadAsync();
        AssertDefaultValues(reloaded);
    }

    [Fact]
    public async Task LoadAsync_WhenFileExists_CopiesPersistedValuesIntoCurrent()
    {
        var path = FilePath();
        var persisted = new PomodoroSettings
        {
            FocusDurationMinutes = 50,
            ShortBreakDurationMinutes = 8,
            LongBreakDurationMinutes = 20,
            CyclesBeforeLongBreak = 6,
            AutoStartNextPhase = false,
            AlwaysOnTop = true,
            SoundEnabled = false,
            NotificationsEnabled = false,
            AppTheme = AppTheme.Light,
            WindowPositionX = 120,
            WindowPositionY = 80,
            WindowWidth = 300,
            WindowHeight = 200,
            MinimizeToTrayOnClose = true
        };
        await new SettingsService(persisted, path).SaveAsync();

        var settings = new PomodoroSettings();
        await new SettingsService(settings, path).LoadAsync();

        Assert.Equal(50, settings.FocusDurationMinutes);
        Assert.Equal(8, settings.ShortBreakDurationMinutes);
        Assert.Equal(20, settings.LongBreakDurationMinutes);
        Assert.Equal(6, settings.CyclesBeforeLongBreak);
        Assert.False(settings.AutoStartNextPhase);
        Assert.True(settings.AlwaysOnTop);
        Assert.False(settings.SoundEnabled);
        Assert.False(settings.NotificationsEnabled);
        Assert.Equal(AppTheme.Light, settings.AppTheme);
        Assert.Equal(120, settings.WindowPositionX);
        Assert.Equal(80, settings.WindowPositionY);
        Assert.Equal(300, settings.WindowWidth);
        Assert.Equal(200, settings.WindowHeight);
        Assert.True(settings.MinimizeToTrayOnClose);
    }

    [Fact]
    public async Task SaveAsync_WritesCurrentSettingsToJson()
    {
        var path = FilePath();
        var settings = new PomodoroSettings
        {
            FocusDurationMinutes = 30,
            AlwaysOnTop = true,
            AppTheme = AppTheme.Light
        };
        var service = new SettingsService(settings, path);

        await service.SaveAsync();

        var json = await File.ReadAllTextAsync(path);
        Assert.Contains("\"FocusDurationMinutes\": 30", json);
        Assert.Contains("\"AlwaysOnTop\": true", json);
        Assert.Contains("Light", json);
    }

    [Fact]
    public async Task LoadAsync_WhenJsonIsInvalid_RestoresDefaultsAndRewritesFile()
    {
        var path = FilePath();
        await File.WriteAllTextAsync(path, "{ this is not valid json");
        var settings = new PomodoroSettings
        {
            FocusDurationMinutes = 99,
            AutoStartNextPhase = false
        };
        var service = new SettingsService(settings, path);

        await service.LoadAsync();

        AssertDefaultValues(settings);
        var json = await File.ReadAllTextAsync(path);
        Assert.Contains("\"FocusDurationMinutes\": 25", json);
    }

    [Fact]
    public async Task LoadAsync_WhenFileIsEmpty_RestoresDefaults()
    {
        var path = FilePath();
        await File.WriteAllTextAsync(path, string.Empty);
        var settings = new PomodoroSettings { FocusDurationMinutes = 40 };
        var service = new SettingsService(settings, path);

        await service.LoadAsync();

        AssertDefaultValues(settings);
    }

    [Fact]
    public async Task LoadAsync_WhenPropertyIsMissing_UsesDefaultForThatProperty()
    {
        var path = FilePath();
        await File.WriteAllTextAsync(path, """{"FocusDurationMinutes":40}""");
        var settings = new PomodoroSettings();
        var service = new SettingsService(settings, path);

        await service.LoadAsync();

        Assert.Equal(40, settings.FocusDurationMinutes);
        Assert.Equal(5, settings.ShortBreakDurationMinutes);
        Assert.Equal(AppTheme.Dark, settings.AppTheme);
        Assert.True(settings.AutoStartNextPhase);
    }

    [Fact]
    public async Task SaveAsync_RaisesSettingsChanged()
    {
        var path = FilePath();
        var service = new SettingsService(new PomodoroSettings(), path);
        var raised = 0;
        service.SettingsChanged += (_, _) => raised++;

        await service.SaveAsync();

        Assert.Equal(1, raised);
    }

    private string FilePath() => Path.Combine(_directory, "settings.json");

    private static void AssertDefaultValues(PomodoroSettings settings)
    {
        Assert.Equal(25, settings.FocusDurationMinutes);
        Assert.Equal(5, settings.ShortBreakDurationMinutes);
        Assert.Equal(15, settings.LongBreakDurationMinutes);
        Assert.Equal(4, settings.CyclesBeforeLongBreak);
        Assert.True(settings.AutoStartNextPhase);
        Assert.False(settings.AlwaysOnTop);
        Assert.True(settings.SoundEnabled);
        Assert.True(settings.NotificationsEnabled);
        Assert.Equal(AppTheme.Dark, settings.AppTheme);
        Assert.Equal(-1, settings.WindowPositionX);
        Assert.Equal(-1, settings.WindowPositionY);
        Assert.Equal(260, settings.WindowWidth);
        Assert.Equal(78, settings.WindowHeight);
        Assert.False(settings.MinimizeToTrayOnClose);
    }
}
