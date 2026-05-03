namespace UedConverter.Converter.FileUtils;

public partial class ObjFile
{
    public static class ObjFileReader
    {
        public static ObjFile Read(string[] lines)
        {
            ObjFile data = new();
            foreach (var line in lines)
            {
                string syntax = line[0..2].Trim();
                if (syntax.Equals(FileSyntax.VERTEX))
                {
                    data.Vertexes.Add(V3d.Parse(line[1..], OBJ_NUMBER_SEPARATOR));
                }
                if (syntax.Equals(FileSyntax.FACE))
                {
                    data.AddFace(Face.Parse(line[1..]));
                }
            }
            return data;
        }
    }
}
