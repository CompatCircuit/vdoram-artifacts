using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CompatCircuitProgramming.Gadgets;
public class FieldGreaterEqualThanGadget : IGadget {
    public List<string> GetInputWireNames() => ["left", "right"];
    public List<string> GetOutputWireNames() => ["is_greater_equal_than"];

    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != 2) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        Wire isLessThanWire;
        {
            GadgetInstance ins = new FieldLessThanGadget().ApplyGadget([inputWires[0], inputWires[1]], $"{namePrefix}_[less_than]");
            ins.Save(circuitBoard);
            isLessThanWire = ins.OutputWires[0];
        }

        Wire outputWire;
        {
            GadgetInstance ins = new BoolNotGadget().ApplyGadget([isLessThanWire], $"{namePrefix}_[not]");
            ins.Save(circuitBoard);
            outputWire = ins.OutputWires[0];
        }

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard([outputWire], circuitBoard);
    }
}
