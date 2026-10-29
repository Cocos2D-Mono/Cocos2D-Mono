using System;
using System.IO;
using System.Text;
using System.Xml;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Regression tests for the nullable conversion of the plist classes (C2D-303).
public class PlistTests
{
    [Fact]
    public void Write_ADocumentWithoutARoot_WritesAPlistWithoutARoot()
    {
        // Writing dereferenced the root, so a document without one threw.
        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(builder))
        {
            new PlistDocument().Write(writer);
        }

        Assert.Null(new PlistDocument(builder.ToString()).Root);
    }

    [Fact]
    public void LoadFromXml_APlistWithoutARoot_ClearsTheOldRoot()
    {
        // Loading only set the root when the plist had one, so a document kept the root of the
        // plist it loaded before.
        var document = new PlistDocument("<plist version=\"1.0\"><string>old</string></plist>");

        document.LoadFromXml("<plist version=\"1.0\"></plist>");

        Assert.Null(document.Root);
    }

    [Fact]
    public void Array_FromValuesWithANull_ThrowsInvalidCastException()
    {
        // The exception's message named the value's type, so a null value threw
        // NullReferenceException.
        Assert.Throws<InvalidCastException>(() => new PlistArray(new object?[] { "a", null }));
    }

    [Fact]
    public void LoadFromXmlFile_ABinaryPlistFromAStreamThatReadsInPieces_LoadsIt()
    {
        // The header was read with a single Read, which can return fewer bytes than it was asked
        // for. Then the header didn't match, and the binary plist was read as XML.
        var document = new PlistDocument();

        document.LoadFromXmlFile(new OneByteAtATimeStream(EmptyBinaryDictionary));

        Assert.IsType<PlistDictionary>(document.Root);
    }

    // "bplist00", an empty dictionary and its offset, then the trailer: 1-byte offsets and
    // object references, one object, which is the top one, and the offset table at byte 9.
    private static readonly byte[] EmptyBinaryDictionary =
    {
        (byte)'b', (byte)'p', (byte)'l', (byte)'i', (byte)'s', (byte)'t', (byte)'0', (byte)'0',
        0xD0,
        0x08,
        0, 0, 0, 0, 0, 0, 1, 1,
        0, 0, 0, 0, 0, 0, 0, 1,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 9,
    };

    // A seekable stream that returns one byte from each read, as any stream is allowed to.
    private sealed class OneByteAtATimeStream : MemoryStream
    {
        public OneByteAtATimeStream(byte[] data) : base(data)
        {
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            base.Read(buffer, offset, Math.Min(count, 1));

        public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(buffer.Length, 1)]);
    }
}
