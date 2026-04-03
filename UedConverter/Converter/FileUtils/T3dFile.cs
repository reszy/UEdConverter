using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace UedConverter.Converter.FileUtils
{
    public class T3dFile
    {
        List<Polygon> polygonList = new List<Polygon>();

        public List<Polygon> Polygons { get => polygonList; }

        public T3dFile(List<Polygon> polygons)
        {
            polygonList = polygons;
        }

        public class Polygon
        {
            public List<V3d> Vertexes { get; set; }
            public V3d Origin { get; set; }
            public V3d Normal { get; set; }
            public V3d TextureU { get; set; }
            public V3d TextureV { get; set; }
            public string Texture { get; set; }

            public Polygon()
            {
                Normal = new V3d();
                Origin = new V3d();
                TextureU = new V3d();
                TextureV = new V3d();
                Vertexes = new List<V3d>();
            }

            public void ParseAttributes(string line)
            {
                var attributes = line.Trim().Split(' ');
                foreach (var attribute in attributes)
                {
                    if (attribute.StartsWith(FileSyntax.T3d.P_TEXTURE)) Texture = attribute.Substring(FileSyntax.T3d.P_TEXTURE.Length + 1);
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
            FileBuilder fileBuilder = new FileBuilder();

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

            return fileBuilder.Build().ToArray();
        }

        class FileBuilder
        {
            private List<string> lines;

            public FileBuilder()
            {
                lines = new List<string>
                {
                    Begin(FileSyntax.T3d.POLY_LIST)
                };
            }

            public FileBuilder StartPolygon(params (string key, string value)[] additionalValues)
            {
                var line = new StringBuilder();
                line.Append(GenerateIndentationLevel(1));
                line.Append(Begin(FileSyntax.T3d.POLYGON));
                if(additionalValues.Length > 0)
                {
                    foreach(var (key, value) in additionalValues)
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            line.Append(' ');
                            line.Append(key);
                            line.Append("=");
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

            private string GenerateIndentationLevel(uint level)
            {
                string indentation = "";
                for (uint i = 0; i < level; i++)
                {
                    indentation += "   ";
                }
                return indentation;
            }

            private string GenerateZeros(int number)
            {
                string indentation = "";
                for (uint i = 0; i < number; i++)
                {
                    indentation += "0";
                }
                return indentation;
            }

            private string WriteDouble(double value)
            {
                string formatedValue = String.Format("{0:F6}", value);
                if(formatedValue[0] == '-')
                {
                    formatedValue = formatedValue.Substring(1);
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
}
