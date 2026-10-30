using System.IO;
using System.IO.Compression;

namespace Cocos2DMono.UnitTests;

// Zlib data (RFC 1950) for tests.
internal static class ZlibTestData
{
    // With corruptChecksum, the Adler-32 at the end of the stream no longer matches the data.
    public static byte[] Compress(byte[] data, bool corruptChecksum = false)
    {
        var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            zlib.Write(data, 0, data.Length);
        }
        byte[] bytes = output.ToArray();
        if (corruptChecksum)
        {
            bytes[bytes.Length - 1] ^= 0xFF;
        }
        return bytes;
    }
}
