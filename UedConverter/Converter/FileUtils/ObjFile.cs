using System.Globalization;

namespace UedConverter.Converter.FileUtils;

public class ObjFile
{
    public string ObjectName { get; set; }
    public List<V3d> Vertexes { get; set; }
    public List<V2d> TextureVertexes { get; set; }
    public List<V3d> VertexNormals { get; set; }
    public List<Face> Faces { get; set; }

    public class Face
    {
        public List<Component> faceComponents = [];
        public string? material = null;

        public class Component(int vertexRef, int vertexTextureRef, int vertexNormalRef)
        {
            public int vertexRef = vertexRef;
            public int vertexTextureRef = vertexTextureRef;
            public int vertexNormalRef = vertexNormalRef;
        }

        public Face(string? material = null)
        {
            this.faceComponents = [];
            this.material = material;
        }

        public Face(List<Component> faceComponents, string? material)
        {
            this.faceComponents = faceComponents;
            this.material = material;
        }

        public void AddComponent(Component component)
        {
            faceComponents.Add(component);
        }

        public static Face Parse(string values, string? material = null)
        {
            Face face = new(material);
            string[] splitted = values.Trim().Split(' ');
            foreach (var split in splitted)
            {
                string[] numbers = split.Trim().Split('/');
                face.AddComponent
                (
                    new Component
                    (
                        string.IsNullOrEmpty(numbers[0]) ? 0 : int.Parse(numbers[0]),
                        string.IsNullOrEmpty(numbers[1]) ? 0 : int.Parse(numbers[1]),
                        string.IsNullOrEmpty(numbers[2]) ? 0 : int.Parse(numbers[2])
                    )
                );
            }
            return face;
        }
    }

    public ObjFile()
    {
        ObjectName = "Unnamed";
        Vertexes = [];
        TextureVertexes = [];
        VertexNormals = [];
        Faces = [];
    }

    public void AddFace(Face face)
    {
        Faces.Add(face);
    }

    public string[] Write()
    {
        FileBuilder file = new(ObjectName);

        foreach (var vertex in Vertexes)
        {
            file.AddVertex(vertex);
        }
        foreach (var textureVertex in TextureVertexes)
        {
            file.AddTextureVertex(textureVertex);
        }
        foreach (var vertexNormal in VertexNormals)
        {
            file.AddVertexNormal(vertexNormal);
        }
        foreach (var face in Faces)
        {
            file.AddFace(face);
        }

        return [.. file.Build()];
    }

    class FileBuilder(string objectName)
    {
        private readonly List<string> lines = ["#UedConverter OBJ File", "o " + objectName];

        private string? lastMaterialUsed = null;

        public FileBuilder AddVertex(V3d value)
        {
            string line = "v " + WritePoint(value, 6);
            lines.Add(line);
            return this;
        }

        public FileBuilder AddTextureVertex(V2d value)
        {
            string line = "vt " + WritePoint(value, 6);
            lines.Add(line);
            return this;
        }

        public FileBuilder AddVertexNormal(V3d value)
        {
            string line = "vn " + WritePoint(value, 4);
            lines.Add(line);
            return this;
        }

        public FileBuilder AddFace(Face value)
        {
            if (value.material != lastMaterialUsed)
            {
                lines.Add($"{FileSyntax.Obj.MATERIAL} {value.material}");
                lastMaterialUsed = value.material;
            }

            string line = "f ";
            foreach (var component in value.faceComponents)
            {
                line += ((component.vertexRef == 0) ? "" : component.vertexRef.ToString()) +
                    "/" +
                    ((component.vertexTextureRef == 0) ? "" : component.vertexTextureRef.ToString()) +
                    "/" +
                    ((component.vertexNormalRef == 0) ? "" : component.vertexNormalRef.ToString()) +
                    " ";
            }
            lines.Add(line);
            return this;
        }

        public List<string> Build()
        {
            lines.Add("s off");
            return lines;
        }

        public static string WriteDouble(double value, uint digits)
        {
            return value.ToString($"F{digits}", CultureInfo.InvariantCulture);
        }

        public static string WritePoint(V3d value, uint digits)
        {
            return WriteDouble(value.X, digits) + " " + WriteDouble(value.Y, digits) + " " + WriteDouble(value.Z, digits);
        }

        public static string WritePoint(V2d value, uint digits)
        {
            return WriteDouble(value.X, digits) + " " + WriteDouble(value.Y, digits);
        }
    }
}
