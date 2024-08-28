using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SadPencil.CompatCircuitCore.Extensions;
public class IPEndpointJsonConverter : JsonConverter<IPEndPoint> {
    public override IPEndPoint? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => IPEndPoint.Parse(reader.GetString()!);
    public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}
