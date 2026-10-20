using System.IO;
using System.IO.Compression;

namespace Cocos2DMono.UnitTests;

// Gzip data for tests.
internal static class GzipTestData
{
    // With corruptChecksum, the CRC-32 in the gzip trailer no longer matches the data, so the data
    // still decompresses but fails its integrity check.
    public static byte[] Compress(byte[] data, bool corruptChecksum = false)
    {
        var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            gzip.Write(data, 0, data.Length);
        }
        byte[] bytes = output.ToArray();
        if (corruptChecksum)
        {
            // The trailer is the CRC-32 of the data, then its length, 4 bytes each.
            bytes[bytes.Length - 8] ^= 0xFF;
        }
        return bytes;
    }
}
