using System.Text;

namespace UedConverter.Converter.FileUtils;

public class T3dFile(List<T3dFile.Polygon> polygons)
{
    private readonly List<Polygon> polygonList = polygons;

    public List<Polygon> Polygons { get => polygonList; }

    public class Polygon
    {
        public List<V3d> Vertexes { get; set; } = [];
        public V3d Origin { get; set; } = new();
        public V3d Normal { get; set; } = new();
        public V3d TextureU { get; set; } = new();
        public V3d TextureV { get; set; } = new();
        public string? Texture { get; set; } = null;

        public void ParseAttributes(string line)
        {
            var attributes = line.Trim().Split(' ');
            foreach (var attribute in attributes)
            {
                if (attribute.StartsWith(FileSyntax.T3d.P_TEXTURE)) Texture = attribute[(FileSyntax.T3d.P_TEXTURE.Length + 1)..];
            }
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
            fileBuilder.StartPolygon((FileSyntax.T3d.P_TEXTURE, polygon.Texture));
            fileBuilder.AddParameter(FileSyntax.T3d.ORIGIN, polygon.Origin);
            fileBuilder.AddParameter(FileSyntax.T3d.NORMAL, polygon.Normal);
            fileBuilder.AddParameter(FileSyntax.T3d.TEXTURE_U, polygon.TextureU);
            fileBuilder.AddParameter(FileSyntax.T3d.TEXTURE_V, polygon.TextureV);
            foreach (var vertex in polygon.Vertexes)
            {
                fileBuilder.AddParameter(FileSyntax.T3d.VERTEX, vertex);
            }
            fileBuilder.EndPolygon();
        }

        return [.. fileBuilder.Build()];
    }

    class FileBuilder
    {
        private readonly List<string> lines = [Begin(FileSyntax.T3d.POLY_LIST)];

        public FileBuilder StartPolygon(params (string key, string? value)[] additionalValues)
        {
            var line = new StringBuilder();
            line.Append(GenerateIndentationLevel(1));
            line.Append(Begin(FileSyntax.T3d.POLYGON));
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
            string line = GenerateIndentationLevel(1) + End(FileSyntax.T3d.POLYGON);
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

            lines.Add(End(FileSyntax.T3d.POLY_LIST));
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
