namespace BitScheduleApi.Utility
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class UInt128Converter : JsonConverter<UInt128>
    {
        public override UInt128 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Implement reading if necessary (here we assume numbers are provided as strings)
            // You could also call reader.GetUInt64() if your JSON contains numeric values.
            //
            string s = reader.GetString()!;
            return UInt128.Parse(s);
        }

        public override void Write(Utf8JsonWriter writer, UInt128 value, JsonSerializerOptions options)
        {
            // Write the ulong as a full decimal string.
            // This ensures that even very large ulong values are correctly represented in JSON without loss of precision.
            //
            writer.WriteStringValue(value.ToString("D"));
        }
    }

}
