using System.Linq;
using System.Text;
using Cocos2D;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Tests from replacing the bundled gzip port with System.IO.Compression (C2D-316). The port checked
// the gzip CRC only when its stream was closed, and ZipUtils never closed it, so data with a bad
// checksum decompressed without an error.
public class ZipUtilsTests
{
    private static readonly byte[] Text = Encoding.ASCII.GetBytes("hello world");

    [Fact]
    public void InflateMemory_GzipData_DecompressesIt()
    {
        byte[] gzip = GzipTestData.Compress(Text);
        var output = new byte[64];

        int result = ZipUtils.InflateMemory(gzip, (uint) gzip.Length, output);

        // The result is the output buffer's size rather than the inflated length (C2D-317), so
        // this only checks that it isn't the failure value.
        Assert.NotEqual(0, result);
        Assert.Equal(Text, output.Take(Text.Length));
    }

    [Fact]
    public void InflateMemory_ABadChecksum_ReturnsZero()
    {
        byte[] gzip = GzipTestData.Compress(Text, corruptChecksum: true);

        int result = ZipUtils.InflateMemory(gzip, (uint) gzip.Length, new byte[64]);

        Assert.Equal(0, result);
    }

    [Fact]
    public void InflateMemory_DataThatIsntGzip_ReturnsZero()
    {
        var notGzip = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        int result = ZipUtils.InflateMemory(notGzip, (uint) notGzip.Length, new byte[64]);

        Assert.Equal(0, result);
    }
}
