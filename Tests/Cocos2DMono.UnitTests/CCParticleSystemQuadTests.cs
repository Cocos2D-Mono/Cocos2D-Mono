using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

public class CCParticleSystemQuadTests
{
    [Fact]
    public void SetDisplayFrame_WithoutTexture_SetsTheFramesTexture()
    {
        // The texture check was inverted, so a system with no texture yet read the
        // name of a null texture and threw (C2D-274).
        var system = new CCParticleSystemQuad(1);
        var texture = new CCTexture2D();

        system.SetDisplayFrame(new CCSpriteFrame(texture, new CCRect(0, 0, 1, 1)));

        Assert.Same(texture, system.Texture);
    }

    [Fact]
    public void Texture_SetToNull_ClearsTheTexture()
    {
        // The setter read the new texture's size before checking it, so null threw
        // (C2D-274). The base class already accepted null.
        var system = new CCParticleSystemQuad(1);
        system.Texture = new CCTexture2D();

        system.Texture = null;

        Assert.Null(system.Texture);
    }
}
