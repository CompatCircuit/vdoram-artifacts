using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.CompatCircuits;
using Anonymous.CompatCircuitCore.CompatCircuits.MpcCircuits;
using Anonymous.CompatCircuitCore.CompatCircuits.R1csCircuits;
using Anonymous.CompatCircuitCore.Computation;
using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives;

namespace Anonymous.CompatCircuitProgramming.CircuitElements;
/// <summary>
/// This executor wrapper does not support R1csCircuitWithValues. It's meant for pure MPC execution.
/// </summary>
public class CircuitBoardPureMpcExecutorWrapper : ICircuitBoardMpcExecutorWrapper {
    protected IMpcExecutor CircuitExecutor { get; }

    protected CompatCircuit CompatCircuit { get; }
    protected CompatCircuitSymbols CompatCircuitSymbols { get; }

    protected MpcCircuit MpcCircuit { get; }
    protected IReadOnlyDictionary<string, CompatCircuitWireSymbol> WireNameToWireSymbols { get; }

    protected int GetWireID(string wireName) => this.WireNameToWireSymbols[wireName].WireID;

    protected Dictionary<int, Field> PublicInputValues { get; } = [];
    protected Dictionary<int, MpcValue> PrivateInputValues { get; } = [];

    public void AddPublic(string wireName, Field value) => this.PublicInputValues.Add(this.GetWireID(wireName), value);
    public void AddPrivate(string wireName, MpcValue value) => this.PrivateInputValues.Add(this.GetWireID(wireName), value);

    protected CircuitExecuteResult? ExecuteResult { get; set; } = null;
    public R1csCircuitWithValues GetR1csCircuitWithValues() => throw new NotSupportedException();

    public CircuitBoardPureMpcExecutorWrapper(CompatCircuit compatCircuit, CompatCircuitSymbols compatCircuitSymbols, IMpcExecutor circuitExecutor) {
        this.CompatCircuit = compatCircuit;
        this.CompatCircuitSymbols = compatCircuitSymbols;
        this.CircuitExecutor = circuitExecutor;

        this.WireNameToWireSymbols = compatCircuitSymbols.GetWireNameToWireSymbolsDictionary();

        this.MpcCircuit = new MpcCircuit(compatCircuit);
    }

    public async Task<CircuitExecuteResult> Compute() => await AsyncHelper.TerminateOnException(async () => {
        if (this.ExecuteResult is not null) {
            throw new Exception("Compute() can be called only once");
        }
        this.ExecuteResult = await this.CircuitExecutor.Compute(this.MpcCircuit, this.PublicInputValues, this.PrivateInputValues);
        this.CheckExecuteResult(this.ExecuteResult);
        return this.ExecuteResult;
    });

    protected virtual void CheckExecuteResult(CircuitExecuteResult executeResult) {
        for (int i = 0; i < executeResult.ValueBoard.Count; i++) {
            if (executeResult.ValueBoard[i] is null) {
                throw new Exception($"Wire {i} should not be null");
            }
        }
    }

    public MpcValue GetOutput(string wireName) =>
        this.ExecuteResult is null ? throw new Exception("Please call Compute() first") :
        this.ExecuteResult.ValueBoard[this.GetWireID(wireName)] ?? throw new Exception($"Output wire {wireName} is null");
}
