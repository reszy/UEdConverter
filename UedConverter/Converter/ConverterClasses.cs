namespace UedConverter.Converter;

public enum FileType
{
    [StringValue("Wavefront | *.obj")]
    OBJ,
    [StringValue("UEd Brush | *.t3d")]
    T3D
}

public enum ConversionType
{
    ToObj,
    ToT3D,
}
