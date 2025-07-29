using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.CompatCircuits.R1csCircuits;
using Anonymous.CompatCircuitCore.Computation;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives;

namespace Anonymous.CompatCircuitProgramming.CircuitElements;
public interface ICircuitBoardMpcExecutorWrapper {
    public void AddPublic(string wireName, Field value);
    public void AddPrivate(string wireName, MpcValue value);
    public Task<CircuitExecuteResult> Compute();
    public MpcValue GetOutput(string wireName);
    public R1csCircuitWithValues GetR1csCircuitWithValues();
}
