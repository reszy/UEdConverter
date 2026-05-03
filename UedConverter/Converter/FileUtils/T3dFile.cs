using System.Text;

namespace UedConverter.Converter.FileUtils;

public partial class T3dFile(List<T3dFile.Polygon> polygons)
{
    private const char T3D_NUMBER_SEPARATOR = ',';
    private readonly List<Polygon> polygonList = polygons;

    public List<Polygon> Polygons { get => polygonList; }
    public static class FileSyntax
    {
        public const string POLY_LIST = "PolyList";
        public const string MAP = "Map";
        public const string POLYGON = "Polygon";

        //POLYGON ATTRIBUTES
        public const string P_TEXTURE = "Texture";
        public const string P_LINK = "Link";
        public const string P_FLAGS = "Flags";

        public const string PAN = "Pan";
        //PAN ATTRIBUTES
        public const string PAN_U = "U";
        public const string PAN_V = "V";

        public const string ORIGIN = "Origin";
        public const string NORMAL = "Normal";
        public const string TEXTURE_U = "TextureU";
        public const string TEXTURE_V = "TextureV";
        public const string VERTEX = "Vertex";
    }

    public partial class Polygon
    {
        public List<V3d> Vertexes { get; set; } = [];
        public V3d Origin { get; set; } = new();
        public V3d Normal { get; set; } = new();
        public UVi Pan { get; set; } = new();
        public V3d TextureU { get; set; } = new();
        public V3d TextureV { get; set; } = new();
        public string? Texture { get; set; } = null;

        public void ParseAttributes(string line)
        {
            var attributes = line.Trim().Split(' ');
            foreach (var attribute in attributes)
            {
                if (attribute.StartsWith(FileSyntax.P_TEXTURE)) Texture = attribute[(FileSyntax.P_TEXTURE.Length + 1)..];
            }
        }

        public static UVi ParsePanAttributes(string line)
        {
            int u = 0;
            int v = 0;
            var attributes = line.Trim().Split(' ');
            var PAN_U = FileSyntax.PAN_U;
            var PAN_V = FileSyntax.PAN_V;
            foreach (var attribute in attributes)
            {
                if (attribute.StartsWith(PAN_U) && line[PAN_U.Length] == '=')
                {
                    u = int.Parse(attribute[(PAN_U.Length + 1)..]);
                }
                if (attribute.StartsWith(PAN_V) && line[PAN_V.Length] == '=')
                {
                    v = int.Parse(attribute[(PAN_V.Length + 1)..]);
                }
            }
            return new UVi(u, v);
        }

        public override string ToString()
        {
            string vertexes = "";
            foreach (var vertex in Vertexes)
            {
                if (vertexes.Length > 0)
                    vertexes += ", ";
                vertexes += vertex.ToString();
            }
            return "Origin={" + Origin.ToString() +
                "} Normal={" + Normal.ToString() +
                "} TextureU={" + TextureU.ToString() +
                "} TextureV={" + TextureV.ToString() +
                "} Vertexes=[" + vertexes + "]";
        }
    }

    public string[] Write()
    {
        FileBuilder fileBuilder = new();

        foreach (var polygon in polygonList)
        {
            fileBuilder.StartPolygon((FileSyntax.P_TEXTURE, polygon.Texture));
            fileBuilder.AddParameter(FileSyntax.ORIGIN, polygon.Origin);
            fileBuilder.AddParameter(FileSyntax.NORMAL, polygon.Normal);
            fileBuilder.AddParameter(FileSyntax.TEXTURE_U, polygon.TextureU);
            fileBuilder.AddParameter(FileSyntax.TEXTURE_V, polygon.TextureV);
            foreach (var vertex in polygon.Vertexes)
            {
                fileBuilder.AddParameter(FileSyntax.VERTEX, vertex);
            }
            fileBuilder.EndPolygon();
        }

        return [.. fileBuilder.Build()];
    }

    class FileBuilder
    {
        private readonly List<string> lines = [Begin(FileSyntax.POLY_LIST)];

        public FileBuilder StartPolygon(params (string key, string? value)[] additionalValues)
        {
            var line = new StringBuilder();
            line.Append(GenerateIndentationLevel(1));
            line.Append(Begin(FileSyntax.POLYGON));
            if (additionalValues.Length > 0)
            {
                foreach (var (key, value) in additionalValues)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        line.Append(' ');
                        line.Append(key);
                        line.Append('=');
                        line.Append(value);
                    }
                }
            }
            lines.Add(line.ToString());
            return this;
        }

        public FileBuilder EndPolygon()
        {
            string line = GenerateIndentationLevel(1) + End(FileSyntax.POLYGON);
            lines.Add(line);
            return this;
        }

        public FileBuilder AddParameter(string syntax, V3d value)
        {
            string line = GenerateIndentationLevel(2) + WriteParameter(syntax, value);
            lines.Add(line);
            return this;
        }

        public List<string> Build()
        {

            lines.Add(End(FileSyntax.POLY_LIST));
            return lines;
        }

        private static string GenerateIndentationLevel(uint level)
        {
            string indentation = "";
            for (uint i = 0; i < level; i++)
            {
                indentation += "   ";
            }
            return indentation;
        }

        private static string GenerateZeros(int number)
        {
            string indentation = "";
            for (uint i = 0; i < number; i++)
            {
                indentation += "0";
            }
            return indentation;
        }

        private static string WriteDouble(double value)
        {
            string formatedValue = string.Format("{0:F6}", value);
            if (formatedValue[0] == '-')
            {
                formatedValue = formatedValue[1..];
            }
            return (value < 0 ? '-' : '+') + GenerateZeros(12 - formatedValue.Length) + formatedValue.Replace(',', '.');
        }

        private string WriteParameter(string syntax, V3d point)
        {
            string pointStr = WriteDouble(point.X) + ',' + WriteDouble(point.Y) + ',' + WriteDouble(point.Z);
            return syntax.PadRight(9, ' ') + pointStr;
        }

    }

    public static string Begin(string syntax)
    {
        return "Begin " + syntax;
    }

    public static string End(string syntax)
    {
        return "End " + syntax;
    }
}
