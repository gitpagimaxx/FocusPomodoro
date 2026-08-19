using Microsoft.UI.Dispatching;

namespace FocusPomodoro.Services;

public sealed class DispatcherQueueTicker : IUiTicker
{
    private readonly DispatcherQueue _dispatcher;
    private readonly DispatcherQueueTimer _timer;

    public DispatcherQueueTicker()
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("No DispatcherQueue is associated with the current thread.");
        _timer = _dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => Tick?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Tick;

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    public void Pulse()
    {
        if (_dispatcher.HasThreadAccess)
        {
            Tick?.Invoke(this, EventArgs.Empty);
            return;
        }

        _ = _dispatcher.TryEnqueue(() => Tick?.Invoke(this, EventArgs.Empty));
    }
}
