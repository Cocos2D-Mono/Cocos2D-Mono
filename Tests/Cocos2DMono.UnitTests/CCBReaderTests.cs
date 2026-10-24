using System.IO;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the CCB reader's nullable conversion (C2D-305).
public class CCBReaderTests
{
    private static readonly CCSize ParentSize = new CCSize(100, 100);

    [Fact]
    public void SharedInstance_WorksTheFirstTime()
    {
        // The constructor registers the default loaders, and SharedInstance registered them
        // again, so its first use threw ArgumentException for a duplicate key.
        CCNodeLoaderLibrary.PurgeSharedCCNodeLoaderLibrary();

        CCNodeLoaderLibrary library = CCNodeLoaderLibrary.SharedInstance;

        Assert.NotNull(library.GetCCNodeLoader("CCNode"));
    }

    [Fact]
    public void ParameterlessReader_ReadsAFile()
    {
        // The parameterless constructor left the reader without a node loader library or a
        // sprite sheet list, so reading a file threw NullReferenceException. It uses the shared
        // library now.
        byte[] file = new CcbiWriter()
            .Header(jsControlled: false)
            .Strings("CCNode")
            .UInt(0).SInt(-1)          // No sequences, and none to play automatically.
            .UInt(0)                   // The root node's class: "CCNode".
            .UInt(0)                   // No member variable.
            .UInt(0)                   // No sequences for the node.
            .UInt(0).UInt(0)           // No properties.
            .UInt(0)                   // No children.
            .ToArray();

        CCNode? node = new CCBReader().ReadNodeGraphFromData(file, null, ParentSize);

        Assert.IsType<CCNode>(node);
    }

    [Fact]
    public void JavaScriptControlledFile_RecordsTheOwnerOutlets()
    {
        // The reader never made the list of owner outlet names, so a JavaScript-controlled file
        // with an owner outlet threw NullReferenceException.
        byte[] file = new CcbiWriter()
            .Header(jsControlled: true)
            .Strings("CCNode", "Controller", "titleLabel")
            .UInt(0).SInt(-1)
            .UInt(0)                   // Class: "CCNode".
            .UInt(1)                   // The JavaScript controller: "Controller".
            .UInt(2).UInt(2)           // Assigned to the owner as "titleLabel".
            .UInt(0)
            .UInt(0).UInt(0)
            .UInt(0)
            .ToArray();
        var reader = new CCBReader(CCNodeLoaderLibrary.SharedInstance);

        CCNode? node = reader.ReadNodeGraphFromData(file, new object(), ParentSize);

        Assert.NotNull(node);
        Assert.Equal(new[] { "titleLabel" }, reader.OwnerOutletNames);
        Assert.Same(node, Assert.Single(reader.OwnerOutletNodes));
        Assert.Equal("Controller", reader.AnimationManager.DocumentControllerName);
    }

    [Fact]
    public void Scale9SpriteWithAFrameThatIsNotInTheCache_IsLeftEmpty()
    {
        // The Scale9 sprite loader passed the missing frame on to the sprite, which threw. The
        // other loaders skip a missing frame.
        byte[] file = new CcbiWriter()
            .Header(jsControlled: false)
            .Strings("CCScale9Sprite", "spriteFrame", "sheet.plist", "missing.png")
            .UInt(0).SInt(-1)
            .UInt(0)                   // Class: "CCScale9Sprite".
            .UInt(0)
            .UInt(0)
            .UInt(1).UInt(0)           // One property:
            .UInt((int) CCBPropertyType.SpriteFrame)
            .UInt(1)                   //   "spriteFrame",
            .Byte(0)                   //   for every platform,
            .UInt(2).UInt(3)           //   "missing.png" from "sheet.plist".
            .UInt(0)
            .ToArray();
        var reader = new CCBReader(CCNodeLoaderLibrary.SharedInstance);
        // Mark the sheet as loaded, so the reader looks the frame up without loading a file.
        reader.LoadedSpriteSheet.Add("sheet.plist");

        CCNode? node = reader.ReadNodeGraphFromData(file, null, ParentSize);

        var sprite = Assert.IsType<CCScale9Sprite>(node);
        Assert.Equal(0, sprite.ChildrenCount);
    }

    [Fact]
    public void ReadNodeGraphFromData_WithAnIncompleteHeader_ReturnsNull()
    {
        // The header check read all four magic bytes from a shorter file, which threw
        // IndexOutOfRangeException.
        var reader = new CCBReader(CCNodeLoaderLibrary.SharedInstance);

        Assert.Null(reader.ReadNodeGraphFromData(new[] { (byte) 'i', (byte) 'b', (byte) 'c' }, null, ParentSize));
    }

    [Fact]
    public void CreateSceneWithNodeGraphFromFile_ForAFileItCannotRead_ThrowsInvalidDataException()
    {
        // The reader returns no node for a file it can't read, and the scene added that null as
        // a child.
        var reader = new CCBReader(CCNodeLoaderLibrary.SharedInstance);

        Assert.Throws<InvalidDataException>(() => reader.CreateSceneWithNodeGraphFromFile(""));
    }
}
