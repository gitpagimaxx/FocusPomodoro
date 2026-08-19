using FocusPomodoro.Models;
using FocusPomodoro.Services;
using FocusPomodoro.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Core;
using Windows.Storage;

namespace FocusPomodoro;

public partial class App : Application
{
    private Window? _window;
    private ITrayIconService? _trayIcon;

    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Services = ConfigureServices();

        var settingsService = Services.GetRequiredService<ISettingsService>();
        await settingsService.LoadAsync();

        ToastNotificationPublisher.Register();
        Services.GetRequiredService<INotificationService>().Attach();
        Services.GetRequiredService<ISoundService>().Attach();

        _window = Services.GetRequiredService<MainWindow>();
        _trayIcon = Services.GetRequiredService<ITrayIconService>();
        _trayIcon.Initialize();
        Services.GetRequiredService<MainViewModel>().RefreshTray();
        CoreApplication.Resuming += OnApplicationResuming;
        _window.Activate();
    }

    private static void OnApplicationResuming(object? sender, object e)
    {
        Services.GetRequiredService<IUiTicker>().Pulse();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        string localFolder;
        try
        {
            localFolder = ApplicationData.Current.LocalFolder.Path;
        }
        catch (InvalidOperationException)
        {
            localFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FocusPomodoro");
            Directory.CreateDirectory(localFolder);
        }

        var settingsPath = Path.Combine(localFolder, SettingsService.FileName);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<PomodoroSettings>();
        services.AddSingleton<ISettingsService>(sp =>
            new SettingsService(sp.GetRequiredService<PomodoroSettings>(), settingsPath));
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<ITrayIconService, TrayIconService>();
        services.AddSingleton<IUiTicker, DispatcherQueueTicker>();
        services.AddSingleton<IPomodoroTimerService, PomodoroTimerService>();
        services.AddSingleton<INotificationService>(sp =>
            new NotificationService(
                sp.GetRequiredService<IPomodoroTimerService>(),
                sp.GetRequiredService<PomodoroSettings>(),
                ToastNotificationPublisher.Show));
        services.AddSingleton<ISoundService>(sp =>
            new SoundService(
                sp.GetRequiredService<IPomodoroTimerService>(),
                sp.GetRequiredService<PomodoroSettings>(),
                PhaseSoundPlayer.Play));
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsWindow>();

        return services.BuildServiceProvider();
    }
}