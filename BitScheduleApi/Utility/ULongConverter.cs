namespace BitScheduleApi.Utility
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class ULongConverter : JsonConverter<ulong>
    {
        public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Implement reading if necessary (here we assume numbers are provided as strings)
            // You could also call reader.GetUInt64() if your JSON contains numeric values.
            string s = reader.GetString();
            return ulong.Parse(s);
        }

        public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
        {
            // Write the ulong as a full decimal string.
            writer.WriteStringValue(value.ToString("D"));
        }
    }

}
