namespace UedConverter.Converter.FileUtils;

public partial class ObjFile
{
    public static class ObjFileReader
    {
        public static ObjFile Read(string[] lines)
        {
            ObjFile data = new();
            string? currentMaterial = null;
            foreach (var line in lines)
            {
                var spaceIndex = line.IndexOf(' ');
                if (spaceIndex < 0) continue;

                string syntax = line[0..spaceIndex].Trim();
                string syntaxParams = line[(spaceIndex + 1)..];

                switch (syntax)
                {
                    case FileSyntax.MATERIAL:
                        {
                            currentMaterial = syntaxParams;
                            break;
                        }
                    case FileSyntax.VERTEX:
                        {
                            data.Vertexes.Add(V3d.Parse(syntaxParams, OBJ_NUMBER_SEPARATOR));
                            break;
                        }
                    case FileSyntax.FACE:
                        {
                            var face = Face.Parse(syntaxParams);
                            face.material = currentMaterial;
                            data.AddFace(face);
                            break;
                        }
                    case FileSyntax.NORMAL:
                        {
                            data.VertexNormals.Add(V3d.Parse(syntaxParams, OBJ_NUMBER_SEPARATOR));
                            break;
                        }
                    default: break;
                }
            }
            return data;
        }
    }
}
