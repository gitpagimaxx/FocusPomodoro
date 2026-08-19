using FocusPomodoro.Helpers;
using FocusPomodoro.Models;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class PhaseNotificationCatalogTests
{
    [Fact]
    public void MessageFor_FocusToShortBreak_ReturnsShortBreakPrompt()
    {
        var transition = new PhaseTransition(PomodoroPhase.Focus, PomodoroPhase.ShortBreak, 1);

        Assert.Equal(
            "Sessão de foco concluída. Hora de uma pausa curta.",
            PhaseNotificationCatalog.MessageFor(transition));
    }

    [Fact]
    public void MessageFor_ShortBreakToFocus_ReturnsFocusPrompt()
    {
        var transition = new PhaseTransition(PomodoroPhase.ShortBreak, PomodoroPhase.Focus, 2);

        Assert.Equal(
            "Pausa concluída. Vamos voltar ao foco.",
            PhaseNotificationCatalog.MessageFor(transition));
    }

    [Fact]
    public void MessageFor_FourthFocusToLongBreak_ReturnsLongBreakPrompt()
    {
        var transition = new PhaseTransition(PomodoroPhase.Focus, PomodoroPhase.LongBreak, 4);

        Assert.Equal(
            "Quatro ciclos concluídos. Aproveite a pausa longa.",
            PhaseNotificationCatalog.MessageFor(transition));
    }

    [Fact]
    public void MessageFor_LongBreakToFocus_ReturnsNewCyclePrompt()
    {
        var transition = new PhaseTransition(PomodoroPhase.LongBreak, PomodoroPhase.Focus, 1);

        Assert.Equal(
            "Pausa longa concluída. Vamos iniciar um novo ciclo de foco.",
            PhaseNotificationCatalog.MessageFor(transition));
    }

    [Fact]
    public void Title_IsFocusPomodoro()
    {
        Assert.Equal("FocusPomodoro", PhaseNotificationCatalog.Title);
    }
}
