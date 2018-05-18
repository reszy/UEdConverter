using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UedConverter.Converter.FileUtils
{
    public class ObjFile
    {
        public string ObjectName { get; set; }
        public List<V3d> Vertexes { get; set; }
        public List<V3d> VertexNormals { get; set; }
        public List<Face> Faces { get; set; }

        public class Face
        {
            public List<Component> faceComponents;

            public class Component
            {
                public int vertexRef;
                public int vertexNormalRef;

                public Component(int vertexRef, int vertexNormalRef)
                {
                    this.vertexNormalRef = vertexNormalRef;
                    this.vertexRef = vertexRef;
                }
            }

            public Face()
            {
                this.faceComponents = new List<Component>();
            }

            public Face(List<Component> faceComponents)
            {
                this.faceComponents = faceComponents;
            }

            public void AddComponent(Component component)
            {
                faceComponents.Add(component);
            }

            public static Face Parse(string values)
            {
                Face face = new Face();
                string[] splitted = values.Trim().Split(' ');
                foreach (var split in splitted)
                {
                    string[] numbers = split.Trim().Split('/');
                    face.AddComponent
                    (
                        new Component
                        (
                            (String.IsNullOrEmpty(numbers[0])) ? 0 : Int32.Parse(numbers[0]),
                            (String.IsNullOrEmpty(numbers[1])) ? 0 : Int32.Parse(numbers[1])
                        )
                    );
                }
                return face;
            }
        }

        public ObjFile()
        {
            ObjectName = "Unnamed";
            Vertexes = new List<V3d>();
            VertexNormals = new List<V3d>();
            Faces = new List<Face>();
        }

        public void AddFace(Face face)
        {
            Faces.Add(face);
        }

        public string[] Write()
        {
            FileBuilder file = new FileBuilder(ObjectName);

            foreach (var vertex in Vertexes)
            {
                file.AddVertex(vertex);
            }
            foreach (var vertexNormal in VertexNormals)
            {
                file.AddVertexNormal(vertexNormal);
            }
            foreach (var face in Faces)
            {
                file.AddFace(face);
            }

            return file.Build().ToArray();
        }

        class FileBuilder
        {
            private List<string> lines;

            public FileBuilder(string objectName)
            {
                lines = new List<string>
                {
                    "#UedConverter OBJ File",
                    "o " + objectName
                };
            }

            public FileBuilder AddVertex(V3d value)
            {
                string line = "v " + WritePoint(value, 6);
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
                string line = "f ";
                foreach (var component in value.faceComponents)
                {
                    line += ((component.vertexRef == 0) ? "" : component.vertexRef.ToString()) +
                        "//" +
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

            public string WriteDouble(double value, uint digits)
            {
                return String.Format("{0:F" + digits + "}", value).Replace(',', '.');
            }

            public string WritePoint(V3d value, uint digits)
            {
                return WriteDouble(value.X, digits) + " " + WriteDouble(value.Y, digits) + " " + WriteDouble(value.Z, digits);
            }
        }
    }
}
