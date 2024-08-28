using SadPencil.CompatCircuitProgramming.CircuitElements;

namespace SadPencil.CompatCircuitProgramming.Gadgets;
public class FieldAddGadget(int factorCount = 2) : IGadget {
    public int FactorCount { get; } = factorCount >= 2 ? factorCount : throw new ArgumentOutOfRangeException(nameof(factorCount), "must be a positive integer and no less than 2");
    public List<string> GetInputWireNames() => Enumerable.Range(0, this.FactorCount).Select(i => $"addend_{i}").ToList();
    public List<string> GetOutputWireNames() => ["sum"];
    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != this.FactorCount) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        Wire lastSumWire = inputWires[0];
        for (int i = 1; i < inputWires.Count; i++) {
            Wire leftWire = lastSumWire;
            Wire rightWire = inputWires[i];

            Wire sumWire = Wire.NewOperationResultWire($"{namePrefix}_[field_add]_sum_{i}", layer: int.Max(leftWire.Layer, rightWire.Layer) + 1);
            Operation addOperation = new(OperationType.Addition, inputWires: [leftWire, rightWire], outputWires: [sumWire]);

            circuitBoard.AddNewOperation(addOperation);

            lastSumWire = sumWire;
        }

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard([lastSumWire], circuitBoard);
    }
}

