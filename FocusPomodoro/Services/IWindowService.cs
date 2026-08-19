using Microsoft.UI.Xaml;

namespace FocusPomodoro.Services;

public interface IWindowService
{
    event EventHandler? CloseRequested;

    bool IsHidden { get; }

    void Initialize(Window window, UIElement dragRegion);

    void ApplyAlwaysOnTop(Window window, bool enabled);

    void Hide();

    void ShowAndActivate();

    Task CloseForExitAsync();

    Task PersistBoundsAsync();
}
