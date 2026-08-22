using FocusPomodoro.Helpers;
using Xunit;

namespace FocusPomodoro.Tests;

public sealed class PhaseElapsedTests
{
    [Fact]
    public void FromRemaining_SubtractsRemainingAndClampsToPlanned()
    {
        Assert.Equal(TimeSpan.FromMinutes(10), PhaseElapsed.FromRemaining(
            TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(15)));
        Assert.Equal(TimeSpan.Zero, PhaseElapsed.FromRemaining(
            TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(30)));
        Assert.Equal(TimeSpan.FromMinutes(25), PhaseElapsed.FromRemaining(
            TimeSpan.FromMinutes(25), TimeSpan.Zero));
        Assert.Equal(TimeSpan.Zero, PhaseElapsed.FromRemaining(TimeSpan.Zero, TimeSpan.Zero));
    }
}
