using System.IO;
using System.IO.Compression;
using System.Text;

namespace UedConverter.Image;


public class PngFile(string filename, int width, int height, int bytesPerPixel = 4)
{
    private readonly string filename = filename;
    private readonly int width = width;
    private readonly int height = height;
    private readonly int bytesPerPixel = bytesPerPixel;

    public void SaveImage(byte[] pixels)
    {
        using var fs = File.Open(filename, FileMode.Create);
        using var br = new BinaryWriter(fs);
        var imageSize = width * height * bytesPerPixel;

        //PNG File Header
        br.Write(PngHeader.Signature);
        br.Write(PngHeader.IHDR(width, height));
        
        br.Write(PngHeader.IDAT(pixels));
        br.Write(PngHeader.IEND());
    }
}

public static class PngHeader
{
    internal static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    internal static byte[] IHDR(int width, int height)
    {
        var xWidth = BitConverter.GetBytes(width).Reverse();
        var yWidth = BitConverter.GetBytes(height).Reverse();
        byte[] xValues = [0x08, 0x06, 0x00, 0x00, 0x00];
        var values = xWidth.Concat(yWidth).Concat(xValues).ToArray();
        return Chunk("IHDR", values);
    }

    internal static byte[] IDAT(byte[] data)
    {
        MemoryStream ms = new();

        using (var compressor = new ZLibStream(ms, CompressionMode.Compress, true))
        {
            compressor.Write(data, 0, data.Length);
        }

        return Chunk("IDAT", ms.ToArray());
    }


    internal static byte[] IEND() => Chunk("IEND", []);

    private static byte[] Chunk(string type, byte[] values)
    {
        var length = BitConverter.GetBytes(values.Length).Reverse().ToArray();
        var chunk = Encoding.ASCII.GetBytes(type)
            .Concat(values)
            .ToArray();
        var crc = Crc(chunk, chunk.Length);
        return [.. length, .. chunk, .. BitConverter.GetBytes(crc).Reverse()];
    }

    private static readonly uint[] CrcTable = new uint[256];
    private static bool CrcTableComputed;

    private static void MakeCrcTable()
    {
        for (var n = 0; n < 256; n++)
        {
            var c = (uint)n;
            for (var k = 0; k < 8; k++)
            {
                if ((c & 1) != 0)
                    c = 0xedb88320u ^ (c >> 1);
                else
                    c >>= 1;
            }

            CrcTable[n] = c;
        }

        CrcTableComputed = true;
    }

    private static uint UpdateCrc(uint crc, byte[] buf, int len)
    {
        var c = crc;
        if (!CrcTableComputed)
            MakeCrcTable();

        for (var n = 0; n < len; n++)
        {
            c = CrcTable[(c ^ buf[n]) & 0xff] ^ (c >> 8);
        }

        return c;
    }

    private static uint Crc(byte[] buf, int len)
    {
        return UpdateCrc(0xffffffffu, buf, len) ^ 0xffffffffu;
    }
}
