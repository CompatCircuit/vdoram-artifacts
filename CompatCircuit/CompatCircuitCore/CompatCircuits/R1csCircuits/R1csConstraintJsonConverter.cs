using SadPencil.CompatCircuitCore.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SadPencil.CompatCircuitCore.CompatCircuits.R1csCircuits;
public class R1csConstraintJsonConverter : JsonConverter<R1csConstraint> {
    public override R1csConstraint? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => R1csConstraint.FromString(reader.GetString()!);
    public override void Write(Utf8JsonWriter writer, R1csConstraint value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());

    public static bool Initialized { get; private set; } = false;
    public static void Initialize() {
        if (Initialized) {
            return;
        }
        JsonSerializerHelper.AddJsonConverter(new R1csConstraintJsonConverter());
        Initialized = true;
    }
}
