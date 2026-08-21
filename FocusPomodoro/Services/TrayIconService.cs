using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FocusPomodoro.Services;

public sealed class TrayIconService : ITrayIconService
{
    private const string IconUri = "ms-appx:///Assets/Tomato.png";

    private TaskbarIcon? _icon;
    private MenuFlyoutItem? _timerItem;
    private MenuFlyoutItem? _restartItem;
    private bool _disposed;

    public event EventHandler? ShowRequested;
    public event EventHandler? TimerActionRequested;
    public event EventHandler? RestartRequested;
    public event EventHandler? ResetCycleRequested;
    public event EventHandler? ExitRequested;

    public void Initialize()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_icon is not null)
        {
            return;
        }

        var showItem = CreateItem("Mostrar FocusPomodoro", () => ShowRequested?.Invoke(this, EventArgs.Empty));
        _timerItem = CreateItem("Iniciar", () => TimerActionRequested?.Invoke(this, EventArgs.Empty));
        _restartItem = CreateItem("Reiniciar período", () => RestartRequested?.Invoke(this, EventArgs.Empty));
        _restartItem.IsEnabled = false;
        var resetCycleItem = CreateItem("Reiniciar ciclo", () => ResetCycleRequested?.Invoke(this, EventArgs.Empty));
        var exitItem = CreateItem("Sair completamente", () => ExitRequested?.Invoke(this, EventArgs.Empty));

        var menu = new MenuFlyout();
        menu.Items.Add(showItem);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(_timerItem);
        menu.Items.Add(_restartItem);
        menu.Items.Add(resetCycleItem);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(exitItem);

        _icon = new TaskbarIcon
        {
            ToolTipText = "FocusPomodoro",
            IconSource = new BitmapImage(new Uri(IconUri)),
            ContextFlyout = menu,
            MenuActivation = PopupActivationMode.RightClick,
            LeftClickCommand = new RelayCommand(() => ShowRequested?.Invoke(this, EventArgs.Empty)),
            DoubleClickCommand = new RelayCommand(() => ShowRequested?.Invoke(this, EventArgs.Empty))
        };

        _icon.ForceCreate(enablesEfficiencyMode: false);
    }

    public void Update(string tooltip, string timerActionText, bool canRestart)
    {
        if (_icon is null)
        {
            return;
        }

        _icon.ToolTipText = tooltip;
        if (_timerItem is not null)
        {
            _timerItem.Text = timerActionText;
        }

        if (_restartItem is not null)
        {
            _restartItem.IsEnabled = canRestart;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _icon?.Dispose();
        _icon = null;
        _timerItem = null;
        _restartItem = null;
    }

    private static MenuFlyoutItem CreateItem(string text, Action action)
    {
        var command = new RelayCommand(action);
        return new MenuFlyoutItem
        {
            Text = text,
            Command = command
        };
    }

    private sealed class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute) => _execute = execute;

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();
    }
}
