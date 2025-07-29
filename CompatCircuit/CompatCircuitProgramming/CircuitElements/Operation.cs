namespace Anonymous.CompatCircuitProgramming.CircuitElements;
public class Operation : object {
    // TODO: this Operation class behaves like CompactCircuitOperation. Find a way to refactor codes without introducing CircuitProgramming into CompatCircuit
    public OperationType OperationType { get; }
    public List<Wire> InputWires { get; }
    public List<Wire> OutputWires { get; }

    public int Layer => this.GetLayer();

    private int GetLayer() {
        int layer = this.OutputWires[0].Layer;
        return this.InputWires.Max(wire => wire.Layer) + 1 != layer || this.OutputWires.Any(wire => wire.Layer != layer)
            ? throw new Exception("Unexpected layer number")
            : layer;
    }

    public Operation(OperationType operationType, IEnumerable<Wire> inputWires, IEnumerable<Wire> outputWires) {
        this.OperationType = operationType;
        this.InputWires = inputWires.ToList();
        this.OutputWires = outputWires.ToList();
        _ = this.GetLayer();
    }

    // Note: Operation explicitly extends Object.
    public sealed override bool Equals(object? obj) => base.Equals(obj);
    public sealed override int GetHashCode() => base.GetHashCode();

    private static string OperationTypeToString(OperationType operationType) => operationType switch {
        OperationType.Addition => "ADD",
        OperationType.Multiplication => "MUL",
        OperationType.Inversion => "INV",
        OperationType.BitDecomposition => "BITS",
        _ => throw new Exception($"Unknown operation type {operationType}"),
    };

    public override string ToString() {
        static string GetWireName(Wire wire) => wire.Name ?? "UntitledWire";
        static List<string> GetWiresName(IEnumerable<Wire> wires) => wires.Select(GetWireName).ToList();

        return this.OperationType switch {
            OperationType.Addition or OperationType.Multiplication =>
                OperationTypeToString(this.OperationType) + " " + string.Join(", ", GetWiresName(this.OutputWires)) + " = " + string.Join(this.OperationType == OperationType.Addition ? " + " : " * ", GetWiresName(this.InputWires)),
            OperationType.Inversion or OperationType.BitDecomposition =>
                OperationTypeToString(this.OperationType) + " " + string.Join(", ", GetWiresName(this.OutputWires)) + " from " + string.Join(", ", GetWiresName(this.OutputWires)),
            _ => throw new Exception($"Unknown operation type {this.OperationType}"),
        };
    }
}
