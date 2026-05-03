using UedConverter.Converter.FileUtils;
using static UedConverter.Converter.FileUtils.T3dFile;

namespace UedConverter.Converter;

class O2U_Converter : IUedConverter
{
    private const double toT3dScale = 100.0;
    public string[] Convert(string[] input)
    {
        var data = ObjFile.ObjFileReader.Read(input);
        return ConvertToT3d(data);
    }

    public static string[] ConvertToT3d(ObjFile data)
    {
        List<Polygon> polygons = [];
        foreach(var face in data.Faces)
        {
            List<V3d> vertexes = [];
            foreach (var component in face.faceComponents)
            {
                vertexes.Add(ConvertAxisToT3d(data.Vertexes[component.vertexRef - 1]) * toT3dScale);
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

    private static V3d ConvertAxisToT3d(V3d input)
    {
        return new(input.X, input.Z, input.Y);
    }
}
