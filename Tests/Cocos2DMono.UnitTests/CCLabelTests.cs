using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression test from CCLabel's nullable conversion (C2D-293). A label drawn before any label
// had text or a font threw NullReferenceException uploading the shared atlas, which didn't
// exist yet.
public class CCLabelTests
{
    [Fact]
    public void Draw_BeforeAnyLabelHasAFont_DoesNotThrow()
    {
        var texture = CCLabel.m_pTexture;
        var data = CCLabel.m_pData;
        CCLabel.m_pTexture = null;
        CCLabel.m_pData = null;
        try
        {
            var label = new FontlessLabel();

            var exception = Record.Exception(() => label.Draw());

            Assert.Null(exception);
        }
        finally
        {
            CCLabel.m_pTexture = texture;
            CCLabel.m_pData = data;
        }
    }

    // Stands in for a label made with the parameterless constructor. It gives the label an
    // empty texture atlas without the vertex buffer, which needs a graphics device; with no
    // quads, drawing the batch returns straight away.
    private sealed class FontlessLabel : CCLabel
    {
        protected override bool InitWithString(string? theString, string? fntFile, CCSize dimensions,
            CCTextAlignment hAlignment, CCVerticalTextAlignment vAlignment, CCPoint imageOffset, CCTexture2D? texture)
        {
            m_pobTextureAtlas = new CCTextureAtlas
            {
                Texture = new CCTexture2D(),
                m_pQuads = new CCRawList<CCV3F_C4B_T2F_Quad>()
            };
            // The atlas starts out needing an upload; make sure it still does.
            m_bTextureDirty = true;
            return true;
        }
    }
}
