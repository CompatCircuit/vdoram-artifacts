using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CompatCircuitProgramming.Gadgets;
public class FieldNormGadget : IGadget {
    public List<string> GetInputWireNames() => ["input"];
    public List<string> GetOutputWireNames() => ["bit"];
    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != 1) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        Wire inputWire = inputWires[0];

        GadgetInstance ins1 = new FieldInverseGadget().ApplyGadget([inputWire], $"{namePrefix}_[field_norm]_inv()");
        ins1.Save(circuitBoard);
        Wire inputWireInverse = ins1.OutputWires[0];

        GadgetInstance ins2 = new FieldMulGadget().ApplyGadget([inputWire, inputWireInverse], $"{namePrefix}_[field_norm]_mul()");
        ins2.Save(circuitBoard);
        Wire outputWire = ins2.OutputWires[0];

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard([outputWire], circuitBoard);
    }
}
