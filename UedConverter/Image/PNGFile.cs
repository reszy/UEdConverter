using System.IO;

namespace UedConverter.Image;


public class PNGFile(string filename, int width, int height, int bytesPerPixel = 4)
{
    private readonly string filename = filename;
    private readonly int width = width;
    private readonly int height = height;
    private readonly int bytesPerPixel = bytesPerPixel;

    public void SaveImage(byte[] pixels)
    {
        using var fs = File.Open(filename, FileMode.Create);
        var imageSize = width * height * bytesPerPixel;

        //PNG File Header
        WriteBytes(fs, PngHeader.Signature);
        WriteBytes(fs, PngHeader.IHDR(width, height));
        
        WriteBytes(fs, PngHeader.IDAT(pixels, width, height));
        WriteBytes(fs, PngHeader.IEND());
    }

    private void WriteBytes(FileStream fs, byte[] bytes)
    {
        foreach (var b in bytes)
        {
            fs?.WriteByte(b);
        }
    }
}
