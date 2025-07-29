using Anonymous.CompatCircuitCore.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anonymous.CollaborativeZkVm.ZkPrograms;
public class ZkProgramOpcodeJsonConverter : JsonConverter<ZkProgramOpcode> {
    public override ZkProgramOpcode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => ZkProgramOpcode.FromString(reader.GetString()!);
    public override void Write(Utf8JsonWriter writer, ZkProgramOpcode value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());

    public static bool Initialized { get; private set; } = false;

    public static void Initialize() {
        if (Initialized) {
            return;
        }
        JsonSerializerHelper.AddJsonConverter(new ZkProgramOpcodeJsonConverter());
        Initialized = true;
    }
}
