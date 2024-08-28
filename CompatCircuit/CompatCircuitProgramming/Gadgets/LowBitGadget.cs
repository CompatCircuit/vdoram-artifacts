using SadPencil.CompatCircuitProgramming.CircuitElements;
using System.Diagnostics;

namespace SadPencil.CompatCircuitProgramming.Gadgets;
public class LowBitGadget(int bitCount) : IGadget {
    public int BitCount { get; } = bitCount;
    public List<string> GetInputWireNames() => Enumerable.Range(0, this.BitCount).Select(i => $"input_{i}").ToList();
    public List<string> GetOutputWireNames() => Enumerable.Range(0, this.BitCount).Select(i => $"output_{i}").ToList();
    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != this.BitCount) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        List<Wire> outputWires = [];
        {
            GadgetInstance ins = new LowBitWithCarryGadget(inputWires.Count).ApplyGadget(inputWires, $"{namePrefix}_[low_bit_with_carry]");
            ins.Save(circuitBoard);
            outputWires.AddRange(ins.OutputWires.SkipLast(1));
        }

        Trace.Assert(outputWires.Count == this.BitCount);

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard(outputWires, circuitBoard);
    }
}
