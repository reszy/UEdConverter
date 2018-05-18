using System;
using System.Reflection;

namespace UedConverter
{
    [AttributeUsage(AttributeTargets.Field)]
    public class StringValue : Attribute
    {
        private string value;
        public string Value { get => value; }

        public StringValue(string value)
        {
            this.value = value;
        }

        public static string GetStringValue(Enum value)
        {
            // Get the type
            Type type = value.GetType();

            // Get fieldinfo for this type
            FieldInfo fieldInfo = type.GetField(value.ToString());

            // Get the stringvalue attributes
            StringValue[] attribs = fieldInfo.GetCustomAttributes(
                typeof(StringValue), false) as StringValue[];

            // Return the first if there was a match.
            return attribs.Length > 0 ? attribs[0].Value : null;
        }
    }
}
