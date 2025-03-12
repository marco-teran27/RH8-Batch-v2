using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Commons.Utils
{
    public class TimeOutSettingsConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out int value))
                {
                    return value < 1 ? 1 : value; // Round negatives/zero to 1
                }
                if (reader.TryGetDouble(out double doubleValue))
                {
                    return (int)Math.Ceiling(doubleValue < 1 ? 1 : doubleValue); // Round decimals up
                }
            }
            else if (reader.TokenType == JsonTokenType.String && double.TryParse(reader.GetString(), out double stringValue))
            {
                return (int)Math.Ceiling(stringValue < 1 ? 1 : stringValue); // Handle string numbers
            }
            return 1; // Default for non-numeric
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}