namespace Anonymous.CompatCircuitCore.CompatCircuits;
/// <summary>
/// Contains auxiliary information about a wire in a circuit
/// </summary>
public record CompatCircuitWireSymbol {
    public required int WireID { get; init; }
    public required string? WireName { get; init; }
    public required bool IsPublicOutput { get; init; }
    public required bool IsPrivateOutput { get; init; }
    public required bool IsPublicInput { get; init; }
    public required bool IsPrivateInput { get; init; }
    public required bool IsConstant { get; init; }
}
