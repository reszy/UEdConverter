using Microsoft.VisualStudio.TestTools.UnitTesting;
using UedConverter.Converter.FileUtils;
using static UedConverter.Converter.FileUtils.T3dFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UedConverter.Converter.FileUtils.Tests
{
    [TestClass()]
    public class T3dFileTests
    {

        [TestMethod()]
        public void WriteTest()
        {
            string[] expectedOutput =
                {
                "Begin PolyList",
                "   Begin Polygon",
                "      Origin   +00040.000000,+00128.000000,+00064.000000",
                "      Normal   +00000.000000,+00000.000000,+00001.000000",
                "      TextureU +00000.000000,-00001.000000,+00000.000000",
                "      TextureV +00001.000000,+00000.000000,+00000.000000",
                "      Vertex   +00040.000000,+00128.000000,+00064.000000",
                "      Vertex   -00064.000000,+00128.000000,+00064.000000",
                "      Vertex   +00032.000000,+00088.000000,+00064.000000",
                "   End Polygon",
                "   Begin Polygon",
                "      Origin   -00064.000000,+00128.000000,-00064.000000",
                "      Normal   -00000.075429,-00000.084046,-00000.993603",
                "      TextureU +00000.013884,-00000.996434,+00000.083231",
                "      TextureV -00000.997054,-00000.007518,+00000.076327",
                "      Vertex   -00072.000000,+00125.745941,-00056.802612",
                "      Vertex   +00032.000000,+00126.530083,-00064.764091",
                "      Vertex   +00024.000000,+00085.938553,-00060.723267",
                "   End Polygon",
                "End PolyList"
            };

            List<Polygon> polygons = new List<Polygon>();
            polygons.Add(new Polygon()
            {
                Origin = new V3d(40.0, 128.0, 64.0),
                Normal = new V3d(0.0, 0.0, 1.0),
                TextureU = new V3d(0.0, -1.0, 0.0),
                TextureV = new V3d(1.0, 0.0, -0.0),
                Vertexes = {
                    new V3d(40.0, 128.0, 64.0),
                    new V3d(-64.0, 128.0, 64.0),
                    new V3d(32.0, 88.0, 64.0),
                },
            });
            polygons.Add(new Polygon()
            {
                Origin = new V3d(-00064.000000, +00128.000000, -00064.000000),
                Normal = new V3d(-00000.075429, -00000.084046, -00000.993603),
                TextureU = new V3d(+00000.013884, -00000.996434, +00000.083231),
                TextureV = new V3d(-00000.997054, -00000.007518, +00000.076327),
                Vertexes = {
                    new V3d(-00072.000000,+00125.745941,-00056.802612),
                    new V3d(+00032.000000,+00126.530083,-00064.764091),
                    new V3d(+00024.000000,+00085.938553,-00060.723267)
                }
            });

            T3dFile file = new T3dFile(polygons);
            var result = file.Write();
            CollectionAssert.AreEqual(expectedOutput, result);
        }
    }
}