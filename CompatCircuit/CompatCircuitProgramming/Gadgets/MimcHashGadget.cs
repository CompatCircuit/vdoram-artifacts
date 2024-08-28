using SadPencil.CompatCircuitCore.GlobalConfig;
using SadPencil.CompatCircuitProgramming.CircuitElements;

namespace SadPencil.CompatCircuitProgramming.Gadgets;
public class MimcHashGadget(int preimageCount, MimcEncryptGadget mimcEncryptGadget) : IGadget {
    public MimcEncryptGadget MimcEncryptGadget { get; } = mimcEncryptGadget;
    public int PreimageCount { get; } = preimageCount > 0 ? preimageCount : throw new ArgumentOutOfRangeException(nameof(preimageCount), "must be a positive integer");

    public static MimcHashGadget GetGadgetWithDefaultParams(int preimageCount) => new(preimageCount, MimcEncryptGadget.GetGadgetWithDefaultParams());

    public List<string> GetInputWireNames() => Enumerable.Range(0, this.PreimageCount).Select(i => $"preimage_{i}").ToList();
    public List<string> GetOutputWireNames() => ["digest"];
    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != this.PreimageCount) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        Wire k = Wire.NewConstantWire(ArithConfig.FieldFactory.Zero, $"{namePrefix}_[mimc_hash]_zero");
        circuitBoard.AddNewConstantWire(k);

        for (int i = 0; i < inputWires.Count; i++) {
            // k = mimc_enc(input[i], k)
            GadgetInstance ins = this.MimcEncryptGadget.ApplyGadget([inputWires[i], k], $"{namePrefix}_[mimc_hash]_{i}_mimc_enc()");
            ins.Save(circuitBoard);
            k = ins.OutputWires[0];
        }

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard([k], circuitBoard);
    }
}
