using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UedConverter.Converter
{
    public static class FileSyntax
    {
        public static class Obj
        {
            public const string COMMENT = "#";
            public const string OBJECT_NAME = "o";
            public const string SMOOTH = "s";
            public const string FACE = "f";
            public const string MATERIAL = "usemtl";
            public const string MATERIAL_LIB = "mtllib";
            public const string NORMAL = "vn";
            public const string VERTEX = "v";
        }

        public static class T3d
        {
            public const string POLY_LIST = "PolyList";
            public const string POLYGON = "Polygon";
            public const string ORIGIN = "Origin";
            public const string NORMAL = "Normal";
            public const string TEXTURE_U = "TextureU";
            public const string TEXTURE_V = "TextureV";
            public const string VERTEX = "Vertex";
        }
    }
}
