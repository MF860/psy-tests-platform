using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PsyApi.Controllers.Json
{
    public class LenientNullableDoubleConverter : JsonConverter<double?>
    {
        public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                if (reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetDouble(out var n)) return n;
                    return null;
                }
                if (reader.TokenType == JsonTokenType.String)
                {
                    var s = reader.GetString();
                    if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v))
                        return v;
                    return null; // do not throw; let controller validate
                }
            }
            catch { }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
        {
            if (value.HasValue) writer.WriteNumberValue(value.Value);
            else writer.WriteNullValue();
        }
    }
}

