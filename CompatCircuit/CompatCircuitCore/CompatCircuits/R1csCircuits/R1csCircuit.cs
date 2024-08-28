namespace SadPencil.CompatCircuitCore.CompatCircuits.R1csCircuits;
public class R1csCircuit {
    public required int WireCount { get; init; }
    public required int PublicWireCount { get; init; }
    public required IReadOnlyList<R1csConstraint> ProductConstraints { get; init; }
    public required IReadOnlyList<R1csConstraint> SumConstraints { get; init; }

    public R1csCircuit() { }
}