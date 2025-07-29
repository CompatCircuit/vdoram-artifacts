using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CompatCircuitProgramming.Gadgets;
public class BoolOrGadget : IGadget {
    public List<string> GetInputWireNames() => ["left", "right"];
    public List<string> GetOutputWireNames() => ["output"];

    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != 2) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        Wire andWire;
        {
            GadgetInstance ins = new BoolAndGadget().ApplyGadget(inputWires, $"{namePrefix}_[bool_or]_and()");
            ins.Save(circuitBoard);
            andWire = ins.OutputWires[0];
        }

        Wire negWire;
        {
            GadgetInstance ins = new FieldNegGadget().ApplyGadget([andWire], $"{namePrefix}_[bool_or]_neg()");
            ins.Save(circuitBoard);
            negWire = ins.OutputWires[0];
        }

        Wire output;
        {
            GadgetInstance ins = new FieldAddGadget(3).ApplyGadget([.. inputWires, negWire], $"{namePrefix}_[bool_or]_add()");
            ins.Save(circuitBoard);
            output = ins.OutputWires[0];
        }

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard([output], circuitBoard);
    }
}