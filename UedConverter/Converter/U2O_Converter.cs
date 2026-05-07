using UedConverter.Converter.FileUtils;
using UedConverter.UtxFile;
using static UedConverter.Converter.FileUtils.T3dFile;

namespace UedConverter.Converter;

public class U2O_Converter : IUedConverter
{
    public U2O_Converter(bool loadTextureData)
    {
        textureData = loadTextureData ? TextureSizeDictionary.Load() : [];
    }

    public U2O_Converter(Dictionary<string, USize> textureData)
    {
        this.textureData = textureData;
    }

    private readonly Dictionary<string, USize> textureData;
    private const double toObjScale = 0.01;
    public HashSet<string> MissingTextureData { get; private set; } = [];

    public string[] Convert(string[] input)
    {
        MissingTextureData = [];
        List<Polygon> polygons = T3dFileReader.Read(input);
        var file = ConvertToObj(polygons);
        return file.Write();
    }

    public ObjFile ConvertToObj(List<Polygon> polygons)
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
            foreach (var rawVertex in polygon.Vertexes)
            {
                var vertex = ConvertAxisToObj(rawVertex) * toObjScale;
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
                file.TextureVertexes.Add(ConvertTextureSpace(rawVertex, polygon));
                face.AddComponent(new ObjFile.Face.Component(number, vertexNumber, polygonNumber));
                vertexNumber++;
            }
            file.AddFace(face);
            polygonNumber++;
        }
        return file;
    }

    private static V3d ConvertAxisToObj(V3d input)
    {
        return new(input.X, input.Z, input.Y);
    }

    private V2d ConvertTextureSpace(V3d vertex, Polygon polygon)
    {
        var uScale = 64;
        var vScale = 64;
        var textureU = polygon.TextureU;
        var textureV = polygon.TextureV;
        var origin = polygon.Origin;
        var pan = polygon.Pan;
        if (polygon.Texture != null)
        {
            if (textureData.TryGetValue(polygon.Texture, out var size))
            {
                uScale = size.Width;
                vScale = size.Height;
            }
            else
            {
                MissingTextureData.Add(polygon.Texture);
            }
        }
        var u = (textureU.Dot(vertex - origin) + pan.U) / uScale;
        var v = (textureV.Dot(vertex - origin) + pan.V) / vScale * -1;
        return new V2d(u, v);
    }
    
}
