using System.IO;
using System.Text;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the nullable conversion of the content-loading classes (C2D-303).
public class CCSAXParserTests
{
    [Fact]
    public void DictMaker_FromAPlistWithADataValue_ReadsTheOtherValues()
    {
        // A plist's <data> element has no attributes, so the parser handed its delegator a null
        // buffer and threw NullReferenceException.
        var maker = new CCDictMaker();
        var parser = new CCSAXParser();
        parser.SetDelegator(maker);

        parser.ParseContent(string.Join("\n",
            "<plist version=\"1.0\">",
            "<dict>",
            "<key>name</key>",
            "<string>hero</string>",
            "<key>pixels</key>",
            "<data>AAEC</data>",
            "<key>speed</key>",
            "<integer>3</integer>",
            "</dict>",
            "</plist>"));

        Assert.NotNull(maker.m_pRootDict);
        Assert.Equal("hero", maker.m_pRootDict["name"]);
        Assert.Equal("3", maker.m_pRootDict["speed"]);
    }

    [Fact]
    public void Parse_AMapWithoutAWidth_ThrowsInvalidDataException()
    {
        // The parser read the missing width as null, which threw ArgumentNullException.
        const string tmx = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                           "<map version=\"1.0\" orientation=\"orthogonal\" height=\"2\" tilewidth=\"8\" tileheight=\"8\"></map>";

        var ex = Assert.Throws<InvalidDataException>(
            () => new CCTMXMapInfo(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(tmx)))));
        Assert.Contains("width", ex.Message);
    }
}
