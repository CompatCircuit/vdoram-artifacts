using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CompatCircuitProgramming.Gadgets;
public class BoolNotGadget(int bitCount = 1) : IGadget {
    public int BitCount { get; } = bitCount > 0 ? bitCount : throw new ArgumentOutOfRangeException(nameof(bitCount), "must be a positive integer");
    public List<string> GetInputWireNames() => Enumerable.Range(0, this.BitCount).Select(i => $"input_{i}").ToList();
    public List<string> GetOutputWireNames() => Enumerable.Range(0, this.BitCount).Select(i => $"output_{i}").ToList();
    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != this.BitCount) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();
        Wire oneWire = Wire.NewConstantWire(ArithConfig.FieldFactory.One, $"{namePrefix}_[bool_not]_one");
        circuitBoard.AddNewConstantWire(oneWire);

        List<Wire> outputWires = [];
        for (int i = 0; i < this.BitCount; i++) {
            GadgetInstance ins = new FieldSubGadget().ApplyGadget([oneWire, inputWires[i]], $"{namePrefix}_[bool_not]_sub_{i}");
            ins.Save(circuitBoard);
            outputWires.Add(ins.OutputWires[0]);
        }

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard(outputWires, circuitBoard);
    }
}
