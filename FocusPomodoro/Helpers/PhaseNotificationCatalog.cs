using FocusPomodoro.Models;

namespace FocusPomodoro.Helpers;

public static class PhaseNotificationCatalog
{
    public const string Title = "FocusPomodoro";

    public const string FocusCompleted =
        "Sessão de foco concluída. Hora de uma pausa curta.";

    public const string ShortBreakCompleted =
        "Pausa concluída. Vamos voltar ao foco.";

    public const string FourthFocusCompleted =
        "Quatro ciclos concluídos. Aproveite a pausa longa.";

    public const string LongBreakCompleted =
        "Pausa longa concluída. Vamos iniciar um novo ciclo de foco.";

    public static string? MessageFor(PhaseTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);

        return (transition.CompletedPhase, transition.NextPhase) switch
        {
            (PomodoroPhase.Focus, PomodoroPhase.ShortBreak) => FocusCompleted,
            (PomodoroPhase.Focus, PomodoroPhase.LongBreak) => FourthFocusCompleted,
            (PomodoroPhase.ShortBreak, _) => ShortBreakCompleted,
            (PomodoroPhase.LongBreak, _) => LongBreakCompleted,
            _ => null
        };
    }
}
