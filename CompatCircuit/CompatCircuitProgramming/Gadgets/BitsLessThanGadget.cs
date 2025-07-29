using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CompatCircuitProgramming.Gadgets;
public class BitsLessThanGadget(int bitCount) : IGadget {
    public int BitCount { get; } = bitCount >= 2 ? bitCount : throw new ArgumentOutOfRangeException(nameof(bitCount), "must be a positive integer and no less than 2");
    public List<string> GetInputWireNames() => Enumerable.Range(0, this.BitCount).Select(i => $"left_bit_{i}").Concat(Enumerable.Range(0, this.BitCount).Select(i => $"right_bit_{i}")).ToList();
    public List<string> GetOutputWireNames() => ["is_less_than"];
    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != 2 * this.BitCount) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        List<Wire> leftBits = [];
        for (int i = 0; i < this.BitCount; i++) {
            leftBits.Add(inputWires[i]);
        }

        List<Wire> rightBits = [];
        for (int i = 0; i < this.BitCount; i++) {
            rightBits.Add(inputWires[this.BitCount + i]);
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        // Compare from MSB to LSB
        List<Wire> xorWires = [];
        for (int i = leftBits.Count - 1; i >= 0; i--) {
            GadgetInstance ins = new BoolXorGadget().ApplyGadget([leftBits[i], rightBits[i]], $"{namePrefix}_[bits_less_than]_xor_{i}");
            ins.Save(circuitBoard);
            xorWires.Add(ins.OutputWires[0]);
        }

        // The result comes from the first bit when left XOR right is true, so we call "low bit" to prepare for selection bits
        // Note: the **first** bit here is in order MSB -> LSB
        List<Wire> xorLowBitWires;
        {
            GadgetInstance ins = new LowBitGadget(xorWires.Count).ApplyGadget(xorWires, $"{namePrefix}_[bits_less_than]_lowbit");
            ins.Save(circuitBoard);
            xorLowBitWires = ins.OutputWires.ToList();
        }

        // Reverse selection bits, to fit the order LSB -> MSB
        xorLowBitWires.Reverse();

        // Compose selection bits with left bits
        Wire outputWire;
        {
            GadgetInstance ins = new SelectComposeGadget(xorLowBitWires.Count).ApplyGadget([.. xorLowBitWires, .. rightBits], $"{namePrefix}_[bits_less_than]_compose");
            ins.Save(circuitBoard);
            outputWire = ins.OutputWires[0];
        }

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard([outputWire], circuitBoard);
    }
}
