using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SadPencil.CompatCircuitCore.Arithmetic;
public class FieldJsonConverter(BigInteger fieldSize) : JsonConverter<Field> {
    public BigInteger FieldSize { get; } = fieldSize;

    public override Field? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Field.FromString(reader.GetString()!, this.FieldSize);
    public override void Write(Utf8JsonWriter writer, Field value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}
