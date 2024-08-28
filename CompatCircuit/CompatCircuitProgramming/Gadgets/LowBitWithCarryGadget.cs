using SadPencil.CompatCircuitProgramming.CircuitElements;
using System.Diagnostics;

namespace SadPencil.CompatCircuitProgramming.Gadgets;
public class LowBitWithCarryGadget(int bitCount) : IGadget {
    public int BitCount { get; } = bitCount;
    public List<string> GetInputWireNames() => Enumerable.Range(0, this.BitCount).Select(i => $"input_{i}").ToList();
    public List<string> GetOutputWireNames() => Enumerable.Range(0, this.BitCount).Select(i => $"output_{i}").Concat(["carry"]).ToList();
    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != this.BitCount) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        List<Wire> notInputWires = [];
        for (int i = 0; i < inputWires.Count; i++) {
            GadgetInstance ins = new BoolNotGadget().ApplyGadget([inputWires[i]], $"{namePrefix}_[low_bit]_{i}_not()");
            ins.Save(circuitBoard);
            notInputWires.Add(ins.OutputWires[0]);
        }

        List<Wire> outputWires = [];
        outputWires.Add(inputWires[0]);
        Wire previousAreFalse = notInputWires[0];

        for (int i = 1; i < inputWires.Count; i++) {
            // output[i] = previousAreFalse AND input[i]
            Wire outputWire;
            {
                GadgetInstance ins = new BoolAndGadget().ApplyGadget([previousAreFalse, inputWires[i]], $"{namePrefix}_[low_bit]_{i}_and1()");
                ins.Save(circuitBoard);
                outputWire = ins.OutputWires[0];
            }
            outputWires.Add(outputWire);

            // previousAreFalse = previousAreFalse AND NOT input[i]
            {
                GadgetInstance ins = new BoolAndGadget().ApplyGadget([previousAreFalse, notInputWires[i]], $"{namePrefix}_[low_bit]_{i}_and2()");
                ins.Save(circuitBoard);
                previousAreFalse = ins.OutputWires[0];
            }
        }

        // Add carry bit
        outputWires.Add(previousAreFalse);

        Trace.Assert(outputWires.Count == this.BitCount + 1);

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard(outputWires, circuitBoard);
    }
}