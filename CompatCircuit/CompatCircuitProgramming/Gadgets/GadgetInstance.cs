using SadPencil.CompatCircuitProgramming.CircuitElements;

namespace SadPencil.CompatCircuitProgramming.Gadgets;
public class GadgetInstance {
    public required IReadOnlyList<Wire> OutputWires { get; init; }
    public required IReadOnlyList<Wire> NewConstantWires { get; init; }
    public required IReadOnlyList<Operation> NewOperations { get; init; }

    public GadgetInstance() { }

    public void Save(CircuitBoard circuitBoard) {
        foreach (Wire wire in this.NewConstantWires) {
            circuitBoard.AddWire(wire);
        }
        foreach (Operation operation in this.NewOperations) {
            circuitBoard.AddOperation(operation);
        }
    }

    internal static GadgetInstance NewFromGadgetTempBoard(IEnumerable<Wire> outputWires, GadgetIncompleteCircuitBoard board) =>
        new() {
            OutputWires = outputWires.ToList(),
            NewConstantWires = board.NewConstantWires.ToList(),
            NewOperations = board.NewOperations.ToList(),
        };
}
