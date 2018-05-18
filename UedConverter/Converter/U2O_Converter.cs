using System;
using System.Collections.Generic;
using UedConverter.Converter.FileUtils;
using static UedConverter.Converter.FileUtils.T3dFile;

namespace UedConverter.Converter
{
    class U2O_Converter : IUedConverter
    {
        private const char T3D_NUMBER_SEPARATOR = ',';

        List<Polygon> loadedPolygons = new List<Polygon>();

        public string[] Convert(string[] input)
        {
            List<Polygon> polygons = Read(input);
            return ConvertToObj(polygons);
        }

        private string[] ConvertToObj(List<Polygon> polygons)
        {
            ObjFile file = new ObjFile()
            {
                ObjectName = "ConvertedObject"
            };
            foreach (var polygon in polygons)
            {
                ObjFile.Face face = new ObjFile.Face();
                foreach (var vertex in polygon.Vertexes)
                {
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
                    face.AddComponent(new ObjFile.Face.Component(number, 0));
                }
                file.AddFace(face);
            }
            return file.Write();
        }

        private void InvalidSynaxError(int line, string additional = "")
        {
            additional = String.IsNullOrEmpty(additional) ? "" : additional;
            throw new ConvertionException("Invalid syntax on line " + (line + 1) + ". " + additional);
        }

        private string GetSyntax(string line)
        {
            var trimmed = line.Trim();
            return trimmed.Substring(0, trimmed.IndexOf(' '));
        }

        private string GetNumbers(string line)
        {
            var trimmed = line.Trim();
            return trimmed.Substring(trimmed.IndexOf(' ')).Trim();
        }

        private List<Polygon> Read(string[] input)
        {
            if (!input[0].Trim().Contains(FileUtils.T3dFile.Begin(FileSyntax.T3d.POLY_LIST)))
                InvalidSynaxError(0);

            if (!input[input.Length - 1].Trim().Contains(FileUtils.T3dFile.End(FileSyntax.T3d.POLY_LIST)))
                InvalidSynaxError(input.Length - 1);

            for (int line = 1; line < input.Length; line++)
            {
                if (input[line].Trim().Contains(FileUtils.T3dFile.Begin(FileSyntax.T3d.POLYGON)))
                {
                    Polygon polygon = new Polygon();
                    loadedPolygons.Add(polygon);
                    while (line < input.Length)
                    {
                        if (input[line].Trim().Contains(FileUtils.T3dFile.End(FileSyntax.T3d.POLYGON)))
                            break;

                        try
                        {
                            var syntax = GetSyntax(input[line]);
                            var numbers = GetNumbers(input[line]);
                            switch (syntax)
                            {
                                case FileSyntax.T3d.ORIGIN:
                                    polygon.Origin = V3d.Parse(numbers, T3D_NUMBER_SEPARATOR);
                                    break;
                                case FileSyntax.T3d.NORMAL:
                                    polygon.Normal = V3d.Parse(numbers, T3D_NUMBER_SEPARATOR);
                                    break;
                                case FileSyntax.T3d.TEXTURE_U:
                                    polygon.TextureU = V3d.Parse(numbers, T3D_NUMBER_SEPARATOR);
                                    break;
                                case FileSyntax.T3d.TEXTURE_V:
                                    polygon.TextureV = V3d.Parse(numbers, T3D_NUMBER_SEPARATOR);
                                    break;
                                case FileSyntax.T3d.VERTEX:
                                    polygon.Vertexes.Add(V3d.Parse(numbers, T3D_NUMBER_SEPARATOR));
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            InvalidSynaxError(line, e.Message);
                        }
                        line++;
                    }
                    if (line >= input.Length)
                    {
                        InvalidSynaxError(line, "Cannot find " + End(FileSyntax.T3d.POLYGON));
                    }
                }
            }
            return loadedPolygons;
        }
    }
}
