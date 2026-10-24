using System.Collections.Generic;
using System.Text;

namespace Cocos2DMono.UnitTests;

// Builds small .ccbi files in memory, in the bit-packed format that CCBReader reads (version 5),
// so reader tests don't need content files.
internal sealed class CcbiWriter
{
    private readonly List<byte> _bytes = new List<byte>();

    // Bits used in the last byte; 0 when the next bit starts a new byte.
    private int _bit;

    public CcbiWriter Header(bool jsControlled)
    {
        Raw(Encoding.ASCII.GetBytes("ibcc"));
        UInt(5);
        return Byte(jsControlled ? (byte) 1 : (byte) 0);
    }

    public CcbiWriter Strings(params string[] strings)
    {
        UInt(strings.Length);
        foreach (string s in strings)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(s);
            Byte((byte) (utf8.Length >> 8));
            Byte((byte) utf8.Length);
            Raw(utf8);
        }
        return this;
    }

    public CcbiWriter UInt(int value)
    {
        return Number(value + 1);
    }

    public CcbiWriter SInt(int value)
    {
        return Number(value >= 0 ? 2 * value + 1 : -2 * value);
    }

    public CcbiWriter Byte(byte value)
    {
        return Raw(new[] { value });
    }

    public byte[] ToArray()
    {
        return _bytes.ToArray();
    }

    // The reader's ReadInt: a run of zero bits as long as the number's bit length minus one, then
    // the number's bits from the most significant, then padding to the next byte.
    private CcbiWriter Number(int number)
    {
        int length = 0;
        while ((number >> (length + 1)) != 0)
        {
            length++;
        }
        for (int i = 0; i < length; i++)
        {
            Bit(false);
        }
        for (int i = length; i >= 0; i--)
        {
            Bit(((number >> i) & 1) != 0);
        }
        _bit = 0;
        return this;
    }

    // The reader takes bits from the least significant end of each byte.
    private void Bit(bool value)
    {
        if (_bit == 0)
        {
            _bytes.Add(0);
        }
        if (value)
        {
            _bytes[_bytes.Count - 1] |= (byte) (1 << _bit);
        }
        _bit = (_bit + 1) % 8;
    }

    private CcbiWriter Raw(byte[] bytes)
    {
        _bit = 0;
        _bytes.AddRange(bytes);
        return this;
    }
}
