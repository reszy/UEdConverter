using UedConverter.Converter.FileUtils;
using static UedConverter.Converter.FileUtils.T3dFile;

namespace UedConverter.Converter;

public class O2U_Converter : IUedConverter
{
    private const double toT3dScale = 100.0;
    public string[] Convert(string[] input)
    {
        var data = ObjFile.ObjFileReader.Read(input);
        var file = ConvertToT3d(data);
        return file.Write();
    }

    public T3dFile ConvertToT3d(ObjFile data)
    {
        string? texture = null;
        List<Polygon> polygons = [];
        foreach(var face in data.Faces)
        {
            texture = (face.material ?? texture);
            List<V3d> vertexes = [];
            foreach (var component in face.faceComponents)
            {
                vertexes.Add(ConvertAxisToT3d(data.Vertexes[component.vertexRef - 1]) * toT3dScale);
            }
            Polygon polygon = new()
            {
                Vertexes = vertexes,
                Texture = face.material,
                Normal = data.VertexNormals[face.faceComponents[0].vertexNormalRef - 1]// (vertexes[1] - vertexes[0]).Cross(vertexes[2] - vertexes[0]).Normalize()
            };
            polygons.Add(polygon);
        }
        return new(polygons);
    }

    private static V3d ConvertAxisToT3d(V3d input)
    {
        return new(input.X, input.Z, input.Y);
    }
}
