using SadPencil.CompatCircuitCore.GlobalConfig;
using SadPencil.CompatCircuitProgramming.CircuitElements;

namespace SadPencil.CompatCircuitProgramming.Gadgets;
public class FieldNegGadget : IGadget {
    public List<string> GetInputWireNames() => ["input"];
    public List<string> GetOutputWireNames() => ["output"];
    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != 1) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        // TODO: After Neg operation is implemented in CompatCircuit, directly calls it

        // Now, we just multiply the input with negOne field
        GadgetIncompleteCircuitBoard circuitBoard = new();

        Wire negOneWire = Wire.NewConstantWire(ArithConfig.FieldFactory.NegOne, $"{namePrefix}_[field_neg]_neg_one");
        circuitBoard.AddNewConstantWire(negOneWire);

        GadgetInstance ins = new FieldMulGadget().ApplyGadget([negOneWire, inputWires[0]], $"{namePrefix}_[field_neg]_mul()");
        ins.Save(circuitBoard);
        Wire outputWire = ins.OutputWires[0];

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard([outputWire], circuitBoard);
    }
}
