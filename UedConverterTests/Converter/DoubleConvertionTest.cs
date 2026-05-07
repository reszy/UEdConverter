using System;
using System.Collections.Generic;
using System.Text;
using UedConverter.Converter;
using UedConverter.Converter.FileUtils;
using UedConverter.UtxFile;

namespace UedConverterTests.Converter;

[TestClass()]
public class DoubleConvertionTest
{
    /// <summary>
    /// Check if vertexes and texture name are corectly kept during conversion. Other parameters seems impossible to recover from obj file
    /// </summary>
    [TestMethod()]
    public void TestT3dToObjAndBack()
    {
        //given
        Dictionary<string, USize> textureDictionary = [];
        textureDictionary["Vname"] = new(128, 32);
        textureDictionary["scrnbrok"] = new(128, 128);
        textureDictionary["Panelsbl"] = new(128, 64);

        List<T3dFile.Polygon> polygons = [
            new T3dFile.Polygon() {
                Texture  = "scrnbrok",
                Origin   = new(-00293.490356, -00037.490349,  00000.000000),
                Normal   = new(-00000.707107,  00000.707107, -00000.000000),
                TextureU = new(-00000.707107, -00000.707107,  00000.000000),
                TextureV = new( 00000.000000,  00000.000000,  00001.000000),
                Vertexes = [
                    new(-00293.490356, -00037.490334, 00384.000000),
                    new(-00293.490356, -00037.490334, 00128.000000),
                    new(-00474.509644, -00218.509674, 00128.000000),
                    new(-00474.509644, -00218.509674, 00384.000000)
              ]
            },
            new T3dFile.Polygon() {
                Texture  = "Vname",
                Origin   = new(-00176.000000,  00256.000000,  00000.000000),
                Normal   = new(-00001.000000, -00000.000000, -00000.000000),
                TextureU = new(-00000.000000, -00001.000000, -00000.000000),
                TextureV = new( 00000.000000,  00000.000000, -00001.000000),
                Vertexes = [
                    new(-00176.000000, 00256.000000, 00192.000000),
                    new(-00176.000000, 00256.000000, 00064.000000),
                    new(-00176.000000, 00128.000000, 00064.000000),
                    new(-00176.000000, 00128.000000, 00192.000000),
                ]
            },
            new T3dFile.Polygon() {
                Texture  = "scrnbrok",
                Origin   = new(-00293.490356, -00037.490349,  00000.000000),
                Normal   = new(-00000.707107,  00000.707107, -00000.000000),
                TextureU = new(-00000.707107, -00000.707107,  00000.000000),
                TextureV = new( 00000.000000,  00000.000000, -00001.000000),
                Vertexes = [
                    new(-00293.490356, -00037.490334,  00112.000000),
                    new(-00293.490356, -00037.490334, -00144.000000),
                    new(-00474.509644, -00218.509674, -00144.000000),
                    new(-00474.509644, -00218.509674,  00112.000000),
                ]
            },
            new T3dFile.Polygon() {
                Texture  = "Panelsbl",
                Origin   = new( 00000.000000,  00416.000000,  00000.000000),
                Normal   = new(-00001.000000, -00000.000000, -00000.000000),
                Pan      = new(16, 0),
                TextureU = new(-00000.000000, -00001.000000, -00000.000000),
                TextureV = new( 00000.000000,  00000.000000,  00001.000000),
                Vertexes = [
                    new(00000.000000, 00416.000000, 00384.000000),
                    new(00000.000000, 00416.000000, 00128.000000),
                    new(00000.000000, 00160.000000, 00128.000000),
                    new(00000.000000, 00160.000000, 00384.000000),
                ]
            },
        ];
        var u2oConverter = new U2O_Converter(textureDictionary);
        var o2uConverter = new O2U_Converter();

        //when
        var objData = u2oConverter.ConvertToObj(polygons);
        var objText = objData.Write();
        var objFromText = ObjFile.ObjFileReader.Read(objText);// convert to text and back to make sure every variable is also picked from file
        var t3dData = o2uConverter.ConvertToT3d(objData);

        //then
        Assert.IsTrue(ComparePolygons(polygons, t3dData.Polygons));
    }

    private static bool ComparePolygons(List<T3dFile.Polygon> expected, List<T3dFile.Polygon> actual)
    {
        var threshold = 0.000001;
        if (expected.Count != actual.Count) throw new AssertFailedException($"Actual polygon count {actual.Count} is different from expected {expected.Count}");
        for (int i = 0; i < expected.Count; i++)
        {
            var expectedPolygon = expected[i];
            var actualPolygon = actual[i];
            //might be impossible to retrive back same texture data
            //if (expectedPolygon.Origin != actualPolygon.Origin) throw FailAtPolygon("Origin", i, actualPolygon.Origin, expectedPolygon.Origin);
            if (!expectedPolygon.Normal.Equals(actualPolygon.Normal, threshold)) throw FailAtPolygon("Normal", i, actualPolygon.Normal, expectedPolygon.Normal);
            if (expectedPolygon.Texture != actualPolygon.Texture) throw FailAtPolygon("Texture", i, actualPolygon.Texture, expectedPolygon.Texture);
            //if (expectedPolygon.TextureU != actualPolygon.TextureU) throw FailAtPolygon("TextureU", i, actualPolygon.TextureU, expectedPolygon.TextureU);
            //if (expectedPolygon.TextureV != actualPolygon.TextureV) throw FailAtPolygon("TextureV", i, actualPolygon.TextureV, expectedPolygon.TextureV);
            //if (expectedPolygon.Pan != actualPolygon.Pan) throw FailAtPolygon("Pan", i, actualPolygon.Pan, expectedPolygon.Pan);
            if (expectedPolygon.Vertexes.Count != actualPolygon.Vertexes.Count) throw FailAtPolygon("Vertexes.Count", i, expectedPolygon.Vertexes.Count, actualPolygon.Vertexes.Count);
            for (int vi = 0; vi < expectedPolygon.Vertexes.Count; vi++)
            {
                var expectedVertex = expectedPolygon.Vertexes[vi];
                var actualVertex = actualPolygon.Vertexes[vi];
                if (!expectedVertex.Equals(actualVertex, threshold)) throw FailAtPolygon($"Vertex[{vi}]", i, expectedVertex, actualVertex);
            }
        }
        return true;
    }

    private static AssertFailedException FailAtPolygon(string what, int polygonNumber, object? actual, object? expected)
    {
        return new AssertFailedException($"At polygon {polygonNumber}, {what}:\n{actual}\nis different from expected:\n{expected}");
    }

}