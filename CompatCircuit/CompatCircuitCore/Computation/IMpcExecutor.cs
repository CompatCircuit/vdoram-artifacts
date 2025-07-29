using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.CompatCircuits.MpcCircuits;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives;

namespace Anonymous.CompatCircuitCore.Computation;
public interface IMpcExecutor {
    public Task<CircuitExecuteResult> Compute(MpcCircuit mpcCircuit, IReadOnlyDictionary<int, Field> publicInputValueDict, IReadOnlyDictionary<int, MpcValue> privateInputValueShareDict);
}