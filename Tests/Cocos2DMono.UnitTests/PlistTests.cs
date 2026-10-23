using System;
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
    public void Array_FromValuesWithANull_ThrowsInvalidCastException()
    {
        // The exception's message named the value's type, so a null value threw
        // NullReferenceException.
        Assert.Throws<InvalidCastException>(() => new PlistArray(new object?[] { "a", null }));
    }
}
