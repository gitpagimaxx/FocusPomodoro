using FocusPomodoro.Services;

namespace FocusPomodoro.Tests;

internal sealed class FakeUiTicker : IUiTicker
{
    public event EventHandler? Tick;

    public bool IsStarted { get; private set; }
    public int StartCount { get; private set; }
    public int StopCount { get; private set; }

    public void Start()
    {
        IsStarted = true;
        StartCount++;
    }

    public void Stop()
    {
        IsStarted = false;
        StopCount++;
    }

    public void Pulse() => RaiseTick();

    public void RaiseTick() => Tick?.Invoke(this, EventArgs.Empty);
}
