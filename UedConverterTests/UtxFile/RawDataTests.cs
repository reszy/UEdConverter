using UedConverter.UtxFile;

namespace UedConverterTests.UtxFile
{
    [TestClass()]
    public class RawDataTests
    {
        [TestMethod()]
        public void ShouldReturnPropperHexRepresentation()
        {
            //given
            byte[] bytes = [0xE7, 0xFF, 0xFA, 0x58, 0x00, 0x22, 0x9D, 0xA4, 0x21, 0xE5, 0x00, 0xA2, 0x01, 0x59, 0x69, 0xF8, 0x40, 0x02, 0x0A, 0x40, 0x80, 0x20, 0x19, 0x8F, 0x0E, 0x04, 0x05, 0x19, 0x0E];
            long offset = 0x0037EE5B;
            string expectedResult = "0x0037EE50:                                    E7 FF FA 58 00\r\n0x0037EE60:  22 9D A4 21 E5 00 A2 01  59 69 F8 40 02 0A 40 80\r\n0x0037EE70:  20 19 8F 0E 04 05 19 0E ";

            var rawData = new RawData(bytes, offset);

            //expect
            Assert.AreEqual(expectedResult, rawData.GetText());
        }
    }
}
