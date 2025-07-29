using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CompatCircuitProgramming.Gadgets;
public class FieldEqualsGadget : IGadget {
    public List<string> GetInputWireNames() => ["left", "right"];
    public List<string> GetOutputWireNames() => ["equals"];

    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != 2) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        GadgetInstance ins1 = new FieldSubGadget().ApplyGadget(inputWires, $"{namePrefix}_[field_equals]_sub()");
        ins1.Save(circuitBoard);
        Wire difference = ins1.OutputWires[0];

        GadgetInstance ins2 = new FieldNormGadget().ApplyGadget([difference], $"{namePrefix}_[field_equals]_norm()");
        ins2.Save(circuitBoard);
        Wire normBit = ins2.OutputWires[0];

        GadgetInstance ins3 = new BoolNotGadget().ApplyGadget([normBit], $"{namePrefix}_[field_equals]_not()");
        ins3.Save(circuitBoard);
        Wire resultBit = ins3.OutputWires[0];

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard([resultBit], circuitBoard);
    }
}
