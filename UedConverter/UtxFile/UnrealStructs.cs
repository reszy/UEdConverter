using System.IO;

namespace UedConverter.UtxFile;

public interface IUnrealStruct
{
    string GetText();
    CustomTreeElement CreateCTE();
}

internal class UnrealStructs
{
    public static object? Read(BinaryReader br, string structType)
    {
        return structType switch
        {
            "ADrop" => new ADrop()
            {
                Type = (ADrop.WDrop)br.ReadByte(),
                Depth = br.ReadByte(),
                X = br.ReadByte(),
                Y = br.ReadByte(),
                SpeedX = br.ReadByte(),
                SpeedY = br.ReadByte(),
                ValueC = br.ReadByte(),
                ValueD = br.ReadByte(),
            },
            _ => null
        };
    }

    public class ADrop : IUnrealStruct
    {
        public enum WDrop : byte
        {
            DROP_FixedDepth,
            DROP_PhaseSpot,
            DROP_ShallowSpot,
            DROP_HalfAmpl,
            DROP_RandomMover,
            DROP_FixedRandomSpot,
            DROP_WhirlyThing,
            DROP_BigWhirly,
            DROP_HorizontalLine,
            DROP_VerticalLine,
            DROP_DiagonalLine1,
            DROP_DiagonalLine2,
            DROP_HorizontalOsc,
            DROP_VerticalOsc,
            DROP_DiagonalOsc1,
            DROP_DiagonalOsc2,
            DROP_RainDrops,
            DROP_AreaClamp,
            DROP_LeakyTap,
            DROP_DrippyTap
        }

        public WDrop Type;
        public byte Depth;
        public byte X;
        public byte Y;

        public byte SpeedX;
        public byte SpeedY;
        public byte ValueC;
        public byte ValueD;

        public string GetText()
        {
            return $"ADrop({Enum.GetName(Type)})";
        }

        public CustomTreeElement CreateCTE()
        {
            return new CustomTreeElement("Value", GetText(),
                new CustomTreeElement("Type", $"{(byte)Type} {Enum.GetName(Type)}"),
                CustomTreeElement.FromValue(X),
                CustomTreeElement.FromValue(Y),
                CustomTreeElement.FromValue(SpeedX),
                CustomTreeElement.FromValue(SpeedY),
                CustomTreeElement.FromValue(ValueC),
                CustomTreeElement.FromValue(ValueD)
                );
        }
    }
}
