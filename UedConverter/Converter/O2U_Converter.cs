using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UedConverter.Converter.FileUtils;
using static UedConverter.Converter.FileUtils.T3dFile;

namespace UedConverter.Converter
{
    class O2U_Converter : IUedConverter
    {
        private const char T3D_NUMBER_SEPARATOR = ' ';

        public string[] Convert(string[] input)
        {
            var data = Read(input);
            return ConvertToT3d(data);
        }

        public ObjFile Read(string[] lines)
        {
            ObjFile data = new ObjFile();
            foreach(var line in lines)
            {
                string syntax = line.Substring(0, 2).Trim();
                if (syntax.Equals(FileSyntax.Obj.VERTEX))
                {
                    data.Vertexes.Add(V3d.Parse(line.Substring(1), T3D_NUMBER_SEPARATOR));
                }
                if (syntax.Equals(FileSyntax.Obj.FACE))
                {
                    data.AddFace(ObjFile.Face.Parse(line.Substring(1)));
                }
            }
            return data;
        }

        public string[] ConvertToT3d(ObjFile data)
        {
            List<Polygon> polygons = new List<Polygon>();
            foreach(var face in data.Faces)
            {
                List<V3d> vertexes = new List<V3d>();
                foreach (var component in face.faceComponents)
                {
                    vertexes.Add(data.Vertexes[component.vertexRef - 1]);
                }
                Polygon polygon = new Polygon()
                {
                    Vertexes = vertexes
                };
                polygons.Add(polygon);
            }
            T3dFile file = new T3dFile(polygons);
            return file.Write();
        }
    }
}
