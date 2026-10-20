using System.Linq;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests from the tile maps' nullable conversion (C2D-300). ObjectNamed read every object's name
// with the dictionary indexer, so an object without a name before the one asked for threw
// KeyNotFoundException.
public class CCTMXObjectGroupTests
{
    [Fact]
    public void ObjectNamed_AfterAnObjectWithoutAName_FindsTheObject()
    {
        var group = TmxTestMaps.Parse(
            "<objectgroup name=\"things\"><object x=\"0\" y=\"0\"/><object name=\"spawn\" x=\"8\" y=\"8\"/></objectgroup>")
            .ObjectGroups.Single();

        var spawn = group.ObjectNamed("spawn");

        Assert.NotNull(spawn);
        Assert.Equal("8", spawn["x"]);
        Assert.Null(group.ObjectNamed("exit"));
    }
}
