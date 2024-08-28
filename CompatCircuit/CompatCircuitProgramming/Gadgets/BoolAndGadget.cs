using SadPencil.CompatCircuitProgramming.CircuitElements;

namespace SadPencil.CompatCircuitProgramming.Gadgets;
public class BoolAndGadget(int bitCount = 2) : IGadget {
    public int BitCount { get; } = bitCount >= 2 ? bitCount : throw new ArgumentOutOfRangeException(nameof(bitCount), "must be a positive integer and no less than 2");

    public List<string> GetInputWireNames() => Enumerable.Range(0, this.BitCount).Select(i => $"input_{i}").ToList();
    public List<string> GetOutputWireNames() => ["output"];

    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) => new FieldMulGadget(this.BitCount).ApplyGadget(inputWires, $"{namePrefix}_[bool_and]_mul()");
}
