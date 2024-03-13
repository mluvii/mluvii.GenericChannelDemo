using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace mluvii.GenericChannelDemo.Web.Serialization
{
    public class SafeStringEnumConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (TryParseStringEnum(reader, objectType, out var result))
            {
                return result;
            }

            return base.ReadJson(reader, objectType, existingValue, serializer);
        }

        private bool TryParseStringEnum(JsonReader reader, Type objectType, out object result)
        {
            try
            {
                if (reader.TokenType != JsonToken.String)
                {
                    result = null;
                    return false;
                }

                var stringValue = reader.Value.ToString();
                if (string.IsNullOrEmpty(stringValue))
                {
                    result = null;
                    return false;
                }

                var isNullable = objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>);
                var enumType = isNullable ? Nullable.GetUnderlyingType(objectType) : objectType;
                if (enumType == null)
                {
                    result = null;
                    return false;
                }

                return Enum.TryParse(enumType, stringValue, out result) || Enum.TryParse(enumType, "UNKNOWN", out result);
            }
            catch
            {
                // base class will throw correct exception
                result = null;
                return false;
            }
        }
    }
}
