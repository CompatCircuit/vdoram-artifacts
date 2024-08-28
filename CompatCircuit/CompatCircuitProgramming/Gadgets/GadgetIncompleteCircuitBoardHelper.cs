using SadPencil.CompatCircuitProgramming.CircuitElements;

namespace SadPencil.CompatCircuitProgramming.Gadgets;
internal static class GadgetIncompleteCircuitBoardHelper {
    internal static void Save(this GadgetInstance gadgetInstance, GadgetIncompleteCircuitBoard board) {
        foreach (Wire wire in gadgetInstance.NewConstantWires) {
            board.AddNewConstantWire(wire);
        }
        foreach (Operation operation in gadgetInstance.NewOperations) {
            board.AddNewOperation(operation);
        }
    }
    internal static GadgetInstance NewGadgetInstanceFromGadgetIncompleteCircuitBoard(IEnumerable<Wire> outputWires, GadgetIncompleteCircuitBoard board) =>
        new() {
            OutputWires = outputWires.ToList(),
            NewConstantWires = board.NewConstantWires.ToList(),
            NewOperations = board.NewOperations.ToList(),
        };
}
