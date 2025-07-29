using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anonymous.CompatCircuitCore.Arithmetic;
public class RingJsonConverter(BigInteger ringSize) : JsonConverter<Ring> {
    public BigInteger RingSize { get; } = ringSize;

    public override Ring? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Ring.FromString(reader.GetString()!, this.RingSize);
    public override void Write(Utf8JsonWriter writer, Ring value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}