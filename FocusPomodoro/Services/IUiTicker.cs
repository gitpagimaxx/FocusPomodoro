namespace FocusPomodoro.Services;

public interface IUiTicker
{
    event EventHandler? Tick;
    void Start();
    void Stop();
    void Pulse();
}
