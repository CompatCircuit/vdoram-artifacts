using SadPencil.CompatCircuitProgramming.CircuitElements;

namespace SadPencil.CompatCircuitProgramming.Gadgets;
public class FieldLessEqualThanGadget : IGadget {
    public List<string> GetInputWireNames() => ["left", "right"];
    public List<string> GetOutputWireNames() => ["is_less_equal_than"];

    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != 2) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        Wire isGreaterThanWire;
        {
            GadgetInstance ins = new FieldGreaterThanGadget().ApplyGadget([inputWires[0], inputWires[1]], $"{namePrefix}_[greater_than]");
            ins.Save(circuitBoard);
            isGreaterThanWire = ins.OutputWires[0];
        }

        Wire outputWire;
        {
            GadgetInstance ins = new BoolNotGadget().ApplyGadget([isGreaterThanWire], $"{namePrefix}_[not]");
            ins.Save(circuitBoard);
            outputWire = ins.OutputWires[0];
        }

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard([outputWire], circuitBoard);
    }
}
