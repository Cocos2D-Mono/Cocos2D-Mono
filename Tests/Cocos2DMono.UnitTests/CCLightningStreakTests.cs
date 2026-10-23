using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests from the misc nodes' nullable conversion (C2D-299). A lightning streak made with the
// parameterless constructor has no track, and updating it threw NullReferenceException.
public class CCLightningStreakTests
{
    [Fact]
    public void Update_OnAStreakWithNoTrack_DoesNotThrow()
    {
        var streak = new CCLightningStreak();

        var exception = Record.Exception(() => streak.Update(0.1f));

        Assert.Null(exception);
    }
}
