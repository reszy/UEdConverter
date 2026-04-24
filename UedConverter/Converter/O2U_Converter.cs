using UedConverter.Converter.FileUtils;
using static UedConverter.Converter.FileUtils.T3dFile;

namespace UedConverter.Converter;

class O2U_Converter : IUedConverter
{
    private const char T3D_NUMBER_SEPARATOR = ' ';

    public string[] Convert(string[] input)
    {
        var data = Read(input);
        return ConvertToT3d(data);
    }

    public static ObjFile Read(string[] lines)
    {
        ObjFile data = new();
        foreach(var line in lines)
        {
            string syntax = line[0..2].Trim();
            if (syntax.Equals(FileSyntax.Obj.VERTEX))
            {
                data.Vertexes.Add(V3d.Parse(line[1..], T3D_NUMBER_SEPARATOR));
            }
            if (syntax.Equals(FileSyntax.Obj.FACE))
            {
                data.AddFace(ObjFile.Face.Parse(line[1..]));
            }
        }
        return data;
    }

    public static string[] ConvertToT3d(ObjFile data)
    {
        List<Polygon> polygons = [];
        foreach(var face in data.Faces)
        {
            List<V3d> vertexes = [];
            foreach (var component in face.faceComponents)
            {
                vertexes.Add(data.Vertexes[component.vertexRef - 1]);
            }
            Polygon polygon = new()
            {
                Vertexes = vertexes
            };
            polygons.Add(polygon);
        }
        T3dFile file = new(polygons);
        return file.Write();
    }
}
