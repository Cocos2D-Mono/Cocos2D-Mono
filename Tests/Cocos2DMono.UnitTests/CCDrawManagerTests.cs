using System;
using Cocos2D;
using Microsoft.Xna.Framework;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the nullable conversion of the runtime classes (C2D-303).
public class CCDrawManagerTests
{
    [Fact]
    public void SetOrientation_BeforeInitializeDisplay_ThrowsInvalidOperationException()
    {
        // Setting the orientation used the graphics device manager that InitializeDisplay sets.
        // Without one, as in a CCGameView, it threw NullReferenceException.
        Assert.Throws<InvalidOperationException>(() => CCDrawManager.SetOrientation(DisplayOrientation.Portrait));
    }
}
