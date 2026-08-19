using FocusPomodoro.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace FocusPomodoro.Services;

public sealed class WindowService : IWindowService
{
    private static readonly TimeSpan PersistDebounce = TimeSpan.FromMilliseconds(400);

    private readonly ISettingsService _settingsService;
    private Window? _window;
    private bool _isApplyingLayout;
    private bool _boundsDirty;
    private bool _allowClose;
    private DispatcherQueueTimer? _persistTimer;

    public WindowService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public event EventHandler? CloseRequested;

    public bool IsHidden => _window is { AppWindow.IsVisible: false };

    public void Initialize(Window window, UIElement dragRegion)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(dragRegion);

        Detach();
        _window = window;

        window.ExtendsContentIntoTitleBar = true;
        window.SetTitleBar(dragRegion);
        window.SetResizable(true);
        window.SetMinimizable(false);
        window.SetMaximizable(false);
        ApplyAlwaysOnTop(window, _settingsService.Current.AlwaysOnTop);

        _persistTimer = window.DispatcherQueue.CreateTimer();
        _persistTimer.Interval = PersistDebounce;
        _persistTimer.IsRepeating = false;
        _persistTimer.Tick += OnPersistTimerTick;

        window.AppWindow.Changed += OnAppWindowChanged;
        window.AppWindow.Closing += OnAppWindowClosing;
        window.Closed += OnWindowClosed;
        _settingsService.SettingsChanged += OnSettingsChanged;

        if (window.Content is FrameworkElement root)
        {
            if (root.IsLoaded)
            {
                ApplyLayout();
            }
            else
            {
                root.Loaded += OnRootLoaded;
            }
        }
    }

    public void ApplyAlwaysOnTop(Window window, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.SetAlwaysOnTop(enabled);
    }

    public void Hide()
    {
        if (_window is null)
        {
            return;
        }

        _ = TryCaptureBounds();
        _window.AppWindow.Hide();
        SchedulePersist();
    }

    public void ShowAndActivate()
    {
        if (_window is null)
        {
            return;
        }

        _window.AppWindow.Show();
        _window.Activate();
        ApplyAlwaysOnTop(_window, _settingsService.Current.AlwaysOnTop);
    }

    public async Task CloseForExitAsync()
    {
        _allowClose = true;
        StopPersistTimer();
        _ = TryCaptureBounds();
        await _settingsService.SaveAsync().ConfigureAwait(true);
        _boundsDirty = false;

        _window?.Close();
    }

    public async Task PersistBoundsAsync()
    {
        StopPersistTimer();
        if (_window is null)
        {
            return;
        }

        if (!_boundsDirty && !TryCaptureBounds())
        {
            return;
        }

        _boundsDirty = false;
        await _settingsService.SaveAsync().ConfigureAwait(false);
    }

    private void OnRootLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement root)
        {
            root.Loaded -= OnRootLoaded;
        }

        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (_window is null)
        {
            return;
        }

        var settings = _settingsService.Current;
        var scale = _window.GetRasterizationScale();
        var size = WindowLayout.ToPixelSize(settings.WindowWidth, settings.WindowHeight, scale);
        var workArea = GetPrimaryWorkArea(_window.AppWindow);
        var hadSavedPosition = WindowLayout.HasPersistedPosition(
            settings.WindowPositionX,
            settings.WindowPositionY);
        var savedPosition = new PixelPoint(settings.WindowPositionX, settings.WindowPositionY);
        var position = hadSavedPosition && WindowLayout.IsVisibleOn(savedPosition, size, workArea)
            ? savedPosition
            : WindowLayout.BottomRight(workArea, size);

        _isApplyingLayout = true;
        try
        {
            _window.AppWindow.Resize(new SizeInt32(size.Width, size.Height));
            _window.AppWindow.Move(new PointInt32(position.X, position.Y));
        }
        finally
        {
            _isApplyingLayout = false;
        }

        if (TryCaptureBounds() && !hadSavedPosition)
        {
            SchedulePersist();
        }
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (_isApplyingLayout || _window is null)
        {
            return;
        }

        if (!args.DidPositionChange && !args.DidSizeChange)
        {
            return;
        }

        if (TryCaptureBounds())
        {
            SchedulePersist();
        }
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        if (_window is null)
        {
            return;
        }

        var window = _window;
        var enabled = _settingsService.Current.AlwaysOnTop;
        if (!window.DispatcherQueue.TryEnqueue(() => ApplyAlwaysOnTop(window, enabled)))
        {
            ApplyAlwaysOnTop(window, enabled);
        }
    }

    private void OnPersistTimerTick(DispatcherQueueTimer sender, object args)
    {
        _ = PersistBoundsAsync();
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_allowClose)
        {
            return;
        }

        args.Cancel = true;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        StopPersistTimer();
        if (TryCaptureBounds() || _boundsDirty)
        {
            _settingsService.SaveAsync().GetAwaiter().GetResult();
            _boundsDirty = false;
        }

        Detach();
    }

    private void SchedulePersist()
    {
        if (_persistTimer is null)
        {
            return;
        }

        _persistTimer.Stop();
        _persistTimer.Start();
    }

    private void StopPersistTimer() => _persistTimer?.Stop();

    private bool TryCaptureBounds()
    {
        if (_window is null)
        {
            return false;
        }

        var scale = _window.GetRasterizationScale();
        var size = WindowLayout.ToDips(_window.AppWindow.Size.Width, _window.AppWindow.Size.Height, scale);
        var settings = _settingsService.Current;
        var changed = settings.WindowWidth != size.Width
            || settings.WindowHeight != size.Height
            || settings.WindowPositionX != _window.AppWindow.Position.X
            || settings.WindowPositionY != _window.AppWindow.Position.Y;

        if (!changed)
        {
            return false;
        }

        settings.WindowWidth = size.Width;
        settings.WindowHeight = size.Height;
        settings.WindowPositionX = _window.AppWindow.Position.X;
        settings.WindowPositionY = _window.AppWindow.Position.Y;
        _boundsDirty = true;
        return true;
    }

    private void Detach()
    {
        if (_window is null)
        {
            return;
        }

        StopPersistTimer();
        if (_persistTimer is not null)
        {
            _persistTimer.Tick -= OnPersistTimerTick;
            _persistTimer = null;
        }

        _window.AppWindow.Changed -= OnAppWindowChanged;
        _window.AppWindow.Closing -= OnAppWindowClosing;
        _window.Closed -= OnWindowClosed;
        _settingsService.SettingsChanged -= OnSettingsChanged;

        if (_window.Content is FrameworkElement root)
        {
            root.Loaded -= OnRootLoaded;
        }

        _window = null;
        _boundsDirty = false;
    }

    private static PixelRect GetPrimaryWorkArea(AppWindow appWindow)
    {
        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;
        return new PixelRect(workArea.X, workArea.Y, workArea.Width, workArea.Height);
    }
}
