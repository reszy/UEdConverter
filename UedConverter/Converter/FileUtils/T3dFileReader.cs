using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace UedConverter.Converter.FileUtils;

public partial class T3dFile
{
    public static class T3dFileReader
    {
        public static List<Polygon> Read(string[] input)
        {
            List<Polygon> loadedPolygons = [];
            if (input[0].Trim().Contains(Begin(FileSyntax.MAP)))
                throw new ConvertionException("Map t3d is not supported, as it doesn't contain calculated geometry.");

            if (!input[0].Trim().Contains(Begin(FileSyntax.POLY_LIST)))
                InvalidSyntaxError(0);

            if (!input[^1].Trim().Contains(End(FileSyntax.POLY_LIST)))
                InvalidSyntaxError(input.Length - 1);

            for (int line = 1; line < input.Length; line++)
            {
                if (input[line].Trim().Contains(Begin(FileSyntax.POLYGON)))
                {
                    Polygon polygon = new();
                    polygon.ParseAttributes(input[line]);
                    loadedPolygons.Add(polygon);
                    line++;
                    while (line < input.Length)
                    {
                        if (input[line].Trim().Contains(End(FileSyntax.POLYGON)))
                            break;

                        try
                        {
                            var syntax = GetSyntax(input[line]);
                            var numbers = GetNumbers(input[line]);
                            switch (syntax)
                            {
                                case FileSyntax.ORIGIN:
                                    polygon.Origin = V3d.Parse(numbers, T3D_NUMBER_SEPARATOR);
                                    break;
                                case FileSyntax.NORMAL:
                                    polygon.Normal = V3d.Parse(numbers, T3D_NUMBER_SEPARATOR);
                                    break;
                                case FileSyntax.PAN:
                                    polygon.Pan = Polygon.ParsePanAttributes(numbers);
                                    break;
                                case FileSyntax.TEXTURE_U:
                                    polygon.TextureU = V3d.Parse(numbers, T3D_NUMBER_SEPARATOR);
                                    break;
                                case FileSyntax.TEXTURE_V:
                                    polygon.TextureV = V3d.Parse(numbers, T3D_NUMBER_SEPARATOR);
                                    break;
                                case FileSyntax.VERTEX:
                                    polygon.Vertexes.Add(V3d.Parse(numbers, T3D_NUMBER_SEPARATOR));
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            InvalidSyntaxError(line, e.Message);
                        }
                        line++;
                    }
                    if (line >= input.Length)
                    {
                        InvalidSyntaxError(line, "Cannot find " + End(FileSyntax.POLYGON));
                    }
                }
            }
            return loadedPolygons;
        }

        private static void InvalidSyntaxError(int line, string additional = "")
        {
            additional = string.IsNullOrEmpty(additional) ? "" : additional;
            throw new ConvertionException("Invalid syntax on line " + (line + 1) + ". " + additional);
        }

        private static string GetSyntax(string line)
        {
            var trimmed = line.Trim();
            return trimmed[..trimmed.IndexOf(' ')];
        }

        private static string GetNumbers(string line)
        {
            var trimmed = line.Trim();
            return trimmed[trimmed.IndexOf(' ')..].Trim();
        }
    }
}
