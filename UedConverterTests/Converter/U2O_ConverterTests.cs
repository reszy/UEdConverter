using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UedConverter.Converter
{
    [TestClass()]
    public class U2O_ConverterTests
    {
        

        [TestMethod()]
        public void ReadMapTest()
        {
            string[] fileInput =
                {
                "Begin Map",
                "Begin Actor Class=LevelInfo Name=LevelInfo0",
                "    Level=LevelInfo'MyLevel.LevelInfo0'",
                "    Tag=LevelInfo",
                "    Region=(Zone=LevelInfo'MyLevel.LevelInfo0',iLeaf=-1)",
                "    Location=(X=1120.000000,Y=9600.000000,Z=320.000000)",
                "    Name=LevelInfo0",
                "End Actor",
                "Begin Actor Class=Brush Name=Brush2",
                "    MainScale=(SheerAxis=SHEER_ZX)",
                "    PostScale=(Scale=(X=0.750000),SheerAxis=SHEER_ZX)",
                "    TempScale=(Scale=(X=0.750000),SheerAxis=SHEER_ZX)",
                "    Level=LevelInfo'MyLevel.LevelInfo0'",
                "    Tag=Brush",
                "    Region=(Zone=LevelInfo'MyLevel.LevelInfo0',iLeaf=1662,ZoneNumber=8)",
                "    Location=(X=402.000000,Y=-102.000000,Z=126.000000)",
                "    Begin Brush Name=Brush",
                "       Begin PolyList",
                "          Begin Polygon Item=OUTSIDE Link=0",
                "             Origin   -00064.000000,-00002.000000,+00004.000000",
                "             Normal   +00000.000000,+00000.000000,+00001.000000",
                "             TextureU +00000.000000,+00001.000000,-00000.000000",
                "             TextureV -00001.000000,+00000.000000,+00000.000000",
                "             Vertex   -00064.000000,-00002.000000,+00004.000000",
                "             Vertex   +00064.000000,-00002.000000,+00004.000000",
                "             Vertex   +00064.000000,+00002.000000,+00004.000000",
                "             Vertex   -00064.000000,+00002.000000,+00004.000000",
                "          End Polygon",
                "          Begin Polygon Item=OUTSIDE Link=1",
                "             Origin   -00064.000000,+00002.000000,-00004.000000",
                "             Normal   +00000.000000,+00000.000000,-00001.000000",
                "             TextureU -00000.000000,-00001.000000,-00000.000000",
                "             TextureV -00001.000000,+00000.000000,+00000.000000",
                "             Vertex   -00064.000000,+00002.000000,-00004.000000",
                "             Vertex   +00064.000000,+00002.000000,-00004.000000",
                "             Vertex   +00064.000000,-00002.000000,-00004.000000",
                "             Vertex   -00064.000000,-00002.000000,-00004.000000",
                "          End Polygon",
                "          Begin Polygon Item=OUTSIDE Link=2",
                "             Origin   -00064.000000,+00002.000000,-00004.000000",
                "             Normal   +00000.000000,+00001.000000,+00000.000000",
                "             TextureU +00001.000000,-00000.000000,+00000.000000",
                "             TextureV +00000.000000,+00000.000000,-00001.000000",
                "             Vertex   -00064.000000,+00002.000000,-00004.000000",
                "             Vertex   -00064.000000,+00002.000000,+00004.000000",
                "             Vertex   +00064.000000,+00002.000000,+00004.000000",
                "             Vertex   +00064.000000,+00002.000000,-00004.000000",
                "          End Polygon",
                "          Begin Polygon Item=OUTSIDE Link=3",
                "             Origin   +00064.000000,-00002.000000,-00004.000000",
                "             Normal   +00000.000000,-00001.000000,+00000.000000",
                "             TextureU -00001.000000,-00000.000000,-00000.000000",
                "             TextureV +00000.000000,+00000.000000,-00001.000000",
                "             Vertex   +00064.000000,-00002.000000,-00004.000000",
                "             Vertex   +00064.000000,-00002.000000,+00004.000000",
                "             Vertex   -00064.000000,-00002.000000,+00004.000000",
                "             Vertex   -00064.000000,-00002.000000,-00004.000000",
                "          End Polygon",
                "          Begin Polygon Item=OUTSIDE Link=4",
                "             Origin   +00064.000000,+00002.000000,-00004.000000",
                "             Normal   +00001.000000,+00000.000000,+00000.000000",
                "             TextureU +00000.000000,-00001.000000,+00000.000000",
                "             TextureV +00000.000000,+00000.000000,-00001.000000",
                "             Vertex   +00064.000000,+00002.000000,-00004.000000",
                "             Vertex   +00064.000000,+00002.000000,+00004.000000",
                "             Vertex   +00064.000000,-00002.000000,+00004.000000",
                "             Vertex   +00064.000000,-00002.000000,-00004.000000",
                "          End Polygon",
                "          Begin Polygon Item=OUTSIDE Link=5",
                "             Origin   -00064.000000,-00002.000000,-00004.000000",
                "             Normal   -00001.000000,+00000.000000,+00000.000000",
                "             TextureU +00000.000000,+00001.000000,+00000.000000",
                "             TextureV +00000.000000,+00000.000000,-00001.000000",
                "             Vertex   -00064.000000,-00002.000000,-00004.000000",
                "             Vertex   -00064.000000,-00002.000000,+00004.000000",
                "             Vertex   -00064.000000,+00002.000000,+00004.000000",
                "             Vertex   -00064.000000,+00002.000000,-00004.000000",
                "          End Polygon",
                "       End PolyList",
                "    End Brush",
                "    Brush=Model'MyLevel.Brush'",
                "    PrePivot=(X=64.000000,Y=-2.000000,Z=-4.000000)",
                "    Name=Brush2",
                "End Actor"
            };


            var converter = new U2O_Converter();
            var exceptionThrown = false;
            try
            {
                converter.Convert(fileInput);
            }
            catch (ConvertionException e)
            {
                exceptionThrown = true;
                Assert.IsTrue(e.Message.Contains("Map t3d is not supported"), $"Exception have incorrect message: {e.Message}");
            }
            Assert.IsTrue(exceptionThrown, "Expected exception was not thrown");
        }
    }
}