namespace FocusPomodoro.Helpers;

public enum WindowCloseDecision
{
    HideToTray,
    AskUser,
    Exit
}

public enum WindowCloseChoice
{
    MinimizeToTray,
    Exit,
    Cancel
}

public static class WindowClosePolicy
{
    public static WindowCloseDecision Decide(bool isExiting, bool minimizeToTrayOnClose)
    {
        if (isExiting)
        {
            return WindowCloseDecision.Exit;
        }

        return minimizeToTrayOnClose
            ? WindowCloseDecision.HideToTray
            : WindowCloseDecision.AskUser;
    }
}
