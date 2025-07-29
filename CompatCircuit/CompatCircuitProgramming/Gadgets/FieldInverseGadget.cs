using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CompatCircuitProgramming.Gadgets;
public class FieldInverseGadget : IGadget {
    public List<string> GetInputWireNames() => ["input"];
    public List<string> GetOutputWireNames() => ["inverse"];

    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != 1) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        Wire inputWire = inputWires[0];
        Wire outputWire = Wire.NewOperationResultWire($"{namePrefix}_[field_inv]_inverse", layer: inputWire.Layer + 1);
        Operation invOperation = new(OperationType.Inversion, inputWires: [inputWire], outputWires: [outputWire]);

        return new GadgetInstance() { OutputWires = [outputWire], NewOperations = [invOperation], NewConstantWires = [] };
    }
}
