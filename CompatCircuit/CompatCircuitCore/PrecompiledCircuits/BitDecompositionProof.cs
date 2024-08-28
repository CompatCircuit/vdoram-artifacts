using SadPencil.CompatCircuitCore.CompatCircuits;
using SadPencil.CompatCircuitCore.CompatCircuits.BasicCircuits;
using SadPencil.CompatCircuitCore.Extensions;
using SadPencil.CompatCircuitCore.GlobalConfig;

namespace SadPencil.CompatCircuitCore.PrecompiledCircuits;
public static class BitDecompositionProof {
    public static BasicCircuit Circuit { get; }
    public static CompatCircuitSymbols CompatCircuitSymbols { get; }

    static BitDecompositionProof() {
        using (MemoryStream stream = new(Resources.BitDecompositionProofCircuitFile)) {
            CompatCircuit CompatCircuit = CompatCircuitSerializer.Deserialize(stream);
            Circuit = new BasicCircuit(CompatCircuit);
        }
        using (MemoryStream stream = new(Resources.BitDecompositionProofCircuitSymbolsFile)) {
            CompatCircuitSymbols = JsonSerializerHelper.Deserialize<CompatCircuitSymbols>(stream, JsonConfig.JsonSerializerOptions) ?? throw new Exception("Failed to deserialize CompatCircuitSymbols.");
        }
    }
}
