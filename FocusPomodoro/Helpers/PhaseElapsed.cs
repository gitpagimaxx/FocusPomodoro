namespace FocusPomodoro.Helpers;

public static class PhaseElapsed
{
    public static TimeSpan FromRemaining(TimeSpan planned, TimeSpan remaining)
    {
        if (planned <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var elapsed = planned - remaining;
        if (elapsed < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return elapsed > planned ? planned : elapsed;
    }
}
