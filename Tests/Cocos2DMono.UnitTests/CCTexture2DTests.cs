using System.Runtime.ExceptionServices;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Reinit reloaded a texture every time it was called, so two threads that found a texture had lost
// its image both reloaded it at once, as AddImage's callers on the game thread and AddImageAsync's
// worker could (C2D-367). A texture made without an image has nothing to reload, which needs no
// graphics device, but Reinit still calls its OnReInit, so the tests count those calls.
public class CCTexture2DTests
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    [Fact]
    public void Reinit_WhileAnotherThreadReloadsTheTexture_ReturnsWithoutReloadingIt()
    {
        var texture = new CCTexture2D();
        using var reloading = new ManualResetEventSlim();
        using var finish = new ManualResetEventSlim();
        int reloads = 0;
        texture.OnReInit = () =>
        {
            if (Interlocked.Increment(ref reloads) == 1)
            {
                // Keeps the other thread's reload going until this thread has called Reinit.
                reloading.Set();
                finish.Wait(WaitLimit);
            }
        };

        Exception? error = null;
        var other = new Thread(() =>
        {
            try
            {
                texture.Reinit();
            }
            catch (Exception exception)
            {
                error = exception;
            }
        })
        { IsBackground = true };
        other.Start();
        Assert.True(reloading.Wait(WaitLimit), "The other thread's reload didn't start.");

        texture.Reinit();
        finish.Set();

        Assert.True(other.Join(WaitLimit), "The other thread's reload didn't finish.");
        if (error != null)
        {
            ExceptionDispatchInfo.Throw(error);
        }
        Assert.Equal(1, reloads);
    }

    [Fact]
    public void Reinit_AfterAReloadFinished_ReloadsAgain()
    {
        var texture = new CCTexture2D();
        int reloads = 0;
        texture.OnReInit = () => reloads++;

        texture.Reinit();
        texture.Reinit();

        Assert.Equal(2, reloads);
    }

    [Fact]
    public void Reinit_AfterAReloadThrew_ReloadsAgain()
    {
        var texture = new CCTexture2D();
        int reloads = 0;
        texture.OnReInit = () =>
        {
            if (++reloads == 1)
            {
                throw new InvalidOperationException("Thrown by the test.");
            }
        };

        Assert.Throws<InvalidOperationException>(texture.Reinit);
        texture.Reinit();

        Assert.Equal(2, reloads);
    }
}
