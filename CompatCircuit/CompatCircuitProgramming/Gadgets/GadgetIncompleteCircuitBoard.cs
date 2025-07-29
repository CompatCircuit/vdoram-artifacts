using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CompatCircuitProgramming.Gadgets;
/// <summary>
/// This board stores new generated wires and operations without validating whether input wires exist
/// </summary>
internal class GadgetIncompleteCircuitBoard {
    private readonly List<Wire> _constantWires = [];
    public IReadOnlyList<Wire> NewConstantWires => this._constantWires;

    private readonly List<Operation> _operations = [];
    public IReadOnlyList<Operation> NewOperations => this._operations;

    private HashSet<string> AllWireNamesHashSet { get; } = [];
    private HashSet<Wire> AllWiresHashSet { get; } = [];
    private HashSet<Operation> AllOperationsHashSet { get; } = [];

    public void AddNewConstantWire(Wire wire) {
        if (this.AllWiresHashSet.Contains(wire)) {
            throw new InvalidOperationException("Wire already exists");
        }
        if (wire.Name is not null) {
            if (this.AllWireNamesHashSet.Contains(wire.Name)) {
                throw new InvalidOperationException("Wire name already exists");
            }
        }

        if (wire.WireType == WireType.OperationResult) {
            throw new ArgumentException("Wire of type OperationResult should be added via AddOperation()", nameof(wire));
        }
        if (wire.WireType != WireType.Constant) {
            throw new ArgumentException("Only constant wires should be added", nameof(wire));
        }

        if (wire.Name is not null) {
            _ = this.AllWireNamesHashSet.Add(wire.Name);
        }

        this._constantWires.Add(wire);
        _ = this.AllWiresHashSet.Add(wire);
    }

    public void AddNewOperation(Operation operation) {
        foreach (Wire wire in operation.OutputWires) {
            if (this.AllWiresHashSet.Contains(wire)) {
                throw new InvalidOperationException("Output wire already exists");
            }
            if (wire.WireType != WireType.OperationResult) {
                throw new ArgumentException("Output wire should be of type OperationResult", nameof(operation));
            }
        }

        if (this.AllOperationsHashSet.Contains(operation)) {
            throw new InvalidOperationException("Operation already exists");
        }

        this._operations.Add(operation);
        _ = this.AllOperationsHashSet.Add(operation);
        foreach (Wire wire in operation.OutputWires) {
            _ = this.AllWiresHashSet.Add(wire);
        }
    }
}
