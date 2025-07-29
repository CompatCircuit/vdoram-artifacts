using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CompatCircuitProgramming.Gadgets;
public class FieldMulGadget(int factorCount = 2) : IGadget {
    public int FactorCount { get; } = factorCount >= 2 ? factorCount : throw new ArgumentOutOfRangeException(nameof(factorCount), "must be a positive integer and no less than 2");
    public List<string> GetInputWireNames() => Enumerable.Range(0, this.FactorCount).Select(i => $"factor_{i}").ToList();
    public List<string> GetOutputWireNames() => ["product"];
    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != this.FactorCount) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        Wire lastProductWire = inputWires[0];
        for (int i = 1; i < inputWires.Count; i++) {
            Wire leftWire = lastProductWire;
            Wire rightWire = inputWires[i];

            Wire productWire = Wire.NewOperationResultWire($"{namePrefix}_[field_mul]_product_{i}", layer: int.Max(leftWire.Layer, rightWire.Layer) + 1);
            Operation multiOperation = new(OperationType.Multiplication, inputWires: [leftWire, rightWire], outputWires: [productWire]);

            circuitBoard.AddNewOperation(multiOperation);

            lastProductWire = productWire;
        }

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard([lastProductWire], circuitBoard);
    }
}
