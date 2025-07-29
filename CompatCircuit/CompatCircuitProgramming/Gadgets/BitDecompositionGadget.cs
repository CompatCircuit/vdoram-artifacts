using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CompatCircuitProgramming.Gadgets;
public class BitDecompositionGadget : IGadget {
    public List<string> GetInputWireNames() => ["input"];
    public List<string> GetOutputWireNames() => Enumerable.Range(0, ArithConfig.BitSize).Select(i => $"bit_{i}").ToList();
    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != 1) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        Wire inputWire = inputWires[0];
        List<Wire> outputWires = [];
        for (int i = 0; i < ArithConfig.BitSize; i++) {
            Wire wire = Wire.NewOperationResultWire($"{namePrefix}_[bit_decomposition]_bit_{i}", layer: inputWire.Layer + 1);
            outputWires.Add(wire);
        }

        Operation bitOperation = new(OperationType.BitDecomposition, inputWires: [inputWire], outputWires: outputWires);

        return new GadgetInstance() { OutputWires = outputWires, NewOperations = [bitOperation], NewConstantWires = [] };
    }
}
