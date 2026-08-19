namespace FocusPomodoro.ViewModels;

public static class TrayPresentation
{
    public static string TimerActionText(bool isRunning, bool isPaused) =>
        isPaused ? "Retomar" : isRunning ? "Pausar" : "Iniciar";

    public static bool CanRestart(bool isRunning, bool isPaused) => isRunning || isPaused;

    public static string ToolTip(string phaseText, string remainingText) =>
        $"FocusPomodoro — {phaseText} {remainingText}";
}
