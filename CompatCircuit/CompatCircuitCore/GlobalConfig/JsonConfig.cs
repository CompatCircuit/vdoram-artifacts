using System.Text.Json;

namespace SadPencil.CompatCircuitCore.GlobalConfig;
public static class JsonConfig {
    public static JsonSerializerOptions JsonSerializerOptions => new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
}
