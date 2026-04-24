using System.Reflection;

namespace UedConverter;

[AttributeUsage(AttributeTargets.Field)]
public class StringValue(string value) : Attribute
{
    private readonly string value = value;
    public string Value { get => value; }

    public static string? GetStringValue(Enum value)
    {
        Type type = value.GetType();
        FieldInfo? fieldInfo = type.GetField(value.ToString());
        StringValue[]? attribs = fieldInfo?.GetCustomAttributes(typeof(StringValue), false) as StringValue[] ?? null;
        return (attribs != null && attribs.Length > 0) ? attribs[0].Value : null;
    }
}
