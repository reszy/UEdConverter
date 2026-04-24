using UedConverter.Converter.FileUtils;
using UedConverter.UtxFile;
using static UedConverter.Converter.FileUtils.T3dFile;

namespace UedConverter.Converter;

public class U2O_Converter(bool loadTextureData) : IUedConverter
{
    private const char T3D_NUMBER_SEPARATOR = ',';

    private readonly Dictionary<string, USize> textureData = loadTextureData ? TextureSizeDictionary.Load() : [];
    private readonly List<Polygon> loadedPolygons = [];
    public HashSet<string> MissingTextureData { get; private set; } = [];

    public string[] Convert(string[] input)
    {
        MissingTextureData = [];
        List<Polygon> polygons = Read(input);
        return ConvertToObj(polygons);
    }

    private string[] ConvertToObj(List<Polygon> polygons)
    {
        ObjFile file = new()
        {
            ObjectName = "ConvertedObject"
        };
        int polygonNumber = 1;
        int vertexNumber = 1;
        foreach (var polygon in polygons)
        {
            ObjFile.Face face = new(polygon.Texture);
            file.VertexNormals.Add(polygon.Normal);
            foreach (var vertex in polygon.Vertexes)
            {
                var foundVertex = file.Vertexes.Find(v => v.Equals(vertex));
                int number;
                if (foundVertex == null)
                {
                    file.Vertexes.Add(vertex);
                    number = file.Vertexes.Count;
                }
                else
                {
                    number = file.Vertexes.IndexOf(foundVertex);
                }
                file.TextureVertexes.Add(ConvertTextureSpace(vertex, polygon.Origin, polygon.TextureU, polygon.TextureV, polygon.Texture));
                face.AddComponent(new ObjFile.Face.Component(number, vertexNumber, polygonNumber));
                vertexNumber++;
            }
            file.AddFace(face);
            polygonNumber++;
        }
        return file.Write();
    }

    private V2d ConvertTextureSpace(V3d vertex, V3d origin, V3d textureU, V3d textureV, string? textureName)
    {
        var uScale = 64;
        var vScale = 64;
        if (textureName != null)
        {
            if (textureData.TryGetValue(textureName, out var size))
            {
                uScale = size.Width;
                vScale = size.Height;
            }
            else
            {
                MissingTextureData.Add(textureName);
            }
        }
        var u = textureU.Dot(vertex - origin) / uScale;
        var v = textureV.Dot(vertex - origin) / vScale;
        return new V2d(u, v);
    }

    private static void InvalidSyntaxError(int line, string additional = "")
    {
        additional = string.IsNullOrEmpty(additional) ? "" : additional;
        throw new ConvertionException("Invalid syntax on line " + (line + 1) + ". " + additional);
    }

    private static string GetSyntax(string line)
    {
        var trimmed = line.Trim();
        return trimmed[..trimmed.IndexOf(' ')];
    }

    private static string GetNumbers(string line)
    {
        var trimmed = line.Trim();
        return trimmed[trimmed.IndexOf(' ')..].Trim();
    }

    private List<Polygon> Read(string[] input)
    {
        if (input[0].Trim().Contains(Begin(FileSyntax.T3d.MAP)))
            throw new ConvertionException("Map t3d is not supported, as it doesn't contain calculated geometry.");

        if (!input[0].Trim().Contains(Begin(FileSyntax.T3d.POLY_LIST)))
            InvalidSyntaxError(0);

        if (!input[^1].Trim().Contains(End(FileSyntax.T3d.POLY_LIST)))
            InvalidSyntaxError(input.Length - 1);

        for (int line = 1; line < input.Length; line++)
        {
            if (input[line].Trim().Contains(Begin(FileSyntax.T3d.POLYGON)))
            {
                Polygon polygon = new();
                polygon.ParseAttributes(input[line]);
                loadedPolygons.Add(polygon);
                while (line < input.Length)
                {
                    if (input[line].Trim().Contains(End(FileSyntax.T3d.POLYGON)))
                        break;

                    try
                    {
                        var syntax = GetSyntax(input[line]);
                        var numbers = GetNumbers(input[line]);
                        switch (syntax)
                        {
                            case FileSyntax.T3d.ORIGIN:
                                polygon.Origin = V3d.Parse(numbers, T3D_NUMBER_SEPARATOR);
                                break;
                            case FileSyntax.T3d.NORMAL:
                                polygon.Normal = V3d.Parse(numbers, T3D_NUMBER_SEPARATOR);
                                break;
                            case FileSyntax.T3d.TEXTURE_U:
                                polygon.TextureU = V3d.Parse(numbers, T3D_NUMBER_SEPARATOR);
                                break;
                            case FileSyntax.T3d.TEXTURE_V:
                                polygon.TextureV = V3d.Parse(numbers, T3D_NUMBER_SEPARATOR);
                                break;
                            case FileSyntax.T3d.VERTEX:
                                polygon.Vertexes.Add(V3d.Parse(numbers, T3D_NUMBER_SEPARATOR));
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        InvalidSyntaxError(line, e.Message);
                    }
                    line++;
                }
                if (line >= input.Length)
                {
                    InvalidSyntaxError(line, "Cannot find " + End(FileSyntax.T3d.POLYGON));
                }
            }
        }
        return loadedPolygons;
    }
}
