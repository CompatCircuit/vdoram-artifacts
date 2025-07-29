using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.CompatCircuits.R1csCircuits;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anonymous.CompatCircuitCore.Extensions;
public static class JsonSerializerHelper {
    private static readonly List<JsonConverter> _jsonConverters = [new IPEndpointJsonConverter(), new IPAddressJsonConverter()];
    public static IReadOnlyList<JsonConverter> JsonConverters { get; } = _jsonConverters;
    public static void AddJsonConverter(JsonConverter converter) => _jsonConverters.Add(converter);
    private static JsonSerializerOptions ModifyOptions(JsonSerializerOptions? options) {
        JsonSerializerOptions newOption = options is null ? new JsonSerializerOptions() : new JsonSerializerOptions(options);

        // Note: currently, some converters are added when a static constructor is called
        // This might not be robust enough, so here is an additional check
        Trace.Assert(JsonConverters.Any(converter => converter is FieldJsonConverter));
        Trace.Assert(JsonConverters.Any(converter => converter is R1csConstraintJsonConverter));

        foreach (JsonConverter converter in JsonConverters) {
            newOption.Converters.Add(converter);
        }

        return newOption;
    }

    public static string Serialize<TValue>(TValue value, JsonSerializerOptions? options = null) => JsonSerializer.Serialize(value, ModifyOptions(options));
    public static void Serialize<TValue>(Stream utf8Json, TValue value, JsonSerializerOptions? options = null) => JsonSerializer.Serialize(utf8Json, value, ModifyOptions(options));
    public static TValue? Deserialize<TValue>(string json, JsonSerializerOptions? options = null) => JsonSerializer.Deserialize<TValue>(json, ModifyOptions(options));
    public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? options = null) => JsonSerializer.Deserialize<TValue>(utf8Json, ModifyOptions(options));
    public static TValue? Deserialize<TValue>(ReadOnlySpan<char> json, JsonSerializerOptions? options = null) => JsonSerializer.Deserialize<TValue>(json, ModifyOptions(options));
    public static TValue? Deserialize<TValue>(Stream utf8Json, JsonSerializerOptions? options = null) => JsonSerializer.Deserialize<TValue>(utf8Json, ModifyOptions(options));

}
