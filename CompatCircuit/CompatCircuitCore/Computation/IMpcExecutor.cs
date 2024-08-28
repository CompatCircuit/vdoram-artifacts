using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.CompatCircuits.MpcCircuits;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives;

namespace SadPencil.CompatCircuitCore.Computation;
public interface IMpcExecutor {
    public Task<CircuitExecuteResult> Compute(MpcCircuit mpcCircuit, IReadOnlyDictionary<int, Field> publicInputValueDict, IReadOnlyDictionary<int, MpcValue> privateInputValueShareDict);
}