using System;
using System.ComponentModel;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests from the support helpers' nullable conversion (C2D-301). The type converters for
// CCPoint, CCSize and CCRect unboxed a null value when converting it to a string, which threw
// NullReferenceException. A null value now goes to TypeConverter, which gives an empty string.
public class CCGeometryConverterTests
{
    [Theory]
    [InlineData(typeof(CCPoint), typeof(CCPointConverter))]
    [InlineData(typeof(CCSize), typeof(CCSizeConverter))]
    [InlineData(typeof(CCRect), typeof(CCRectConverter))]
    public void ConvertToString_Null_ReturnsAnEmptyString(Type type, Type converterType)
    {
        TypeConverter converter = TypeDescriptor.GetConverter(type);

        Assert.IsType(converterType, converter);
        Assert.Equal(string.Empty, converter.ConvertToString(null));
    }

    // The non-null path is unchanged. Rects aren't checked here because their strings don't
    // convert back (C2D-315).
    [Fact]
    public void ConvertToString_APointAndASize_StillConvertBack()
    {
        TypeConverter points = TypeDescriptor.GetConverter(typeof(CCPoint));
        TypeConverter sizes = TypeDescriptor.GetConverter(typeof(CCSize));

        Assert.Equal("{1,2}", points.ConvertToString(new CCPoint(1, 2)));
        Assert.Equal(new CCPoint(1, 2), points.ConvertFromString("{1,2}"));
        Assert.Equal("{3,4}", sizes.ConvertToString(new CCSize(3, 4)));
        Assert.Equal(new CCSize(3, 4), sizes.ConvertFromString("{3,4}"));
    }
}
