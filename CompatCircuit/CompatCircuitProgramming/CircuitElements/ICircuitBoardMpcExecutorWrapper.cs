using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.CompatCircuits.R1csCircuits;
using SadPencil.CompatCircuitCore.Computation;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives;

namespace SadPencil.CompatCircuitProgramming.CircuitElements;
public interface ICircuitBoardMpcExecutorWrapper {
    public void AddPublic(string wireName, Field value);
    public void AddPrivate(string wireName, MpcValue value);
    public Task<CircuitExecuteResult> Compute();
    public MpcValue GetOutput(string wireName);
    public R1csCircuitWithValues GetR1csCircuitWithValues();
}
