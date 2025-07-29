using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CompatCircuitProgramming.Gadgets;
public class FieldSubGadget : IGadget {
    public List<string> GetInputWireNames() => ["minuend", "subtrahend"];
    public List<string> GetOutputWireNames() => ["difference"];
    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != 2) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        Wire minuend = inputWires[0];
        Wire subtrahend = inputWires[1];

        GadgetInstance ins1 = new FieldNegGadget().ApplyGadget([subtrahend], $"{namePrefix}_[field_sub]_neg()");
        ins1.Save(circuitBoard);
        Wire subtrahendNeg = ins1.OutputWires[0];

        GadgetInstance ins2 = new FieldAddGadget().ApplyGadget([minuend, subtrahendNeg], $"{namePrefix}_[field_sub]_sub()");
        ins2.Save(circuitBoard);
        Wire difference = ins2.OutputWires[0];

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard([difference], circuitBoard);
    }
}
