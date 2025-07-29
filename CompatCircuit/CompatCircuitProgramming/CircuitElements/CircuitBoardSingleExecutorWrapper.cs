using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.CompatCircuits;
using Anonymous.CompatCircuitCore.CompatCircuits.R1csCircuits.Exceptions;
using Anonymous.CompatCircuitCore.Computation;
using Anonymous.CompatCircuitCore.Computation.SingleParty;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives;

namespace Anonymous.CompatCircuitProgramming.CircuitElements;
public class CircuitBoardSingleExecutorWrapper : CircuitBoardExecutorWrapperBase<Field>, ICircuitBoardMpcExecutorWrapper {
    protected SingleExecutor CircuitExecutor { get; }
    protected CompatCircuitSymbols? DebugSymbols { get; } = null;

    public CircuitBoardSingleExecutorWrapper(CompatCircuit compatCircuit, CompatCircuitSymbols compatCircuitSymbols, SingleExecutor circuitExecutor)
        : base(compatCircuit, compatCircuitSymbols) => this.CircuitExecutor = circuitExecutor;

    public CircuitBoardSingleExecutorWrapper(CompatCircuit compatCircuit, CompatCircuitSymbols compatCircuitSymbols, SingleExecutor circuitExecutor, CompatCircuitSymbols debugSymbols)
        : this(compatCircuit, compatCircuitSymbols, circuitExecutor) => this.DebugSymbols = debugSymbols;

    public static CircuitBoardSingleExecutorWrapper FromNewSingleExecutor(CompatCircuit compatCircuit, CompatCircuitSymbols compatCircuitSymbols) => new(compatCircuit, compatCircuitSymbols, circuitExecutor: new SingleExecutor());

    public static CircuitBoardSingleExecutorWrapper FromNewSingleExecutor(CompatCircuit compatCircuit, CompatCircuitSymbols compatCircuitSymbols, CompatCircuitSymbols debugSymbols) => new(compatCircuit, compatCircuitSymbols, circuitExecutor: new SingleExecutor(), debugSymbols);

    public static CircuitBoardSingleExecutorWrapper FromNewSingleExecutor(CircuitBoard circuitBoard, string circuitName) {
        CircuitBoardConverter.ToCompatCircuit(circuitBoard, circuitName, out CompatCircuit compatCircuit, out CompatCircuitSymbols compatCircuitSymbols, out CompatCircuitSymbols debugSymbols);
        return FromNewSingleExecutor(compatCircuit, compatCircuitSymbols, debugSymbols);
    }

    public void SelfVerify() {
        CompatCircuitCore.CompatCircuits.R1csCircuits.R1csCircuitWithValues r1csCircuitWithValues = this.GetR1csCircuitWithValues();
        try {
            r1csCircuitWithValues.SelfVerify();
        }
        catch (R1csProductConstraintFailedException e) {
            if (this.DebugSymbols is not null) {
                Dictionary<int, CompatCircuitWireSymbol> wireIDToSymbols = this.DebugSymbols.GetWireIDToWireSymbolsDictionary();
                string leftWireName = wireIDToSymbols.GetValueOrDefault(e.LeftWire)?.WireName ?? "<missing>";
                string rightWireName = wireIDToSymbols.GetValueOrDefault(e.RightWire)?.WireName ?? "<missing>";
                string resultWireName = wireIDToSymbols.GetValueOrDefault(e.ResultWire)?.WireName ?? "<missing>";
                string message = $"{e.Message}\nleft: {leftWireName}\nright: {rightWireName}\nresult: {resultWireName})";
                throw new R1csProductConstraintFailedException(message, e.LeftWire, e.RightWire, e.ResultWire);
            }
            throw;
        }
        catch (R1csSumConstraintFailedException e) {
            if (this.DebugSymbols is not null) {
                Dictionary<int, CompatCircuitWireSymbol> wireIDToSymbols = this.DebugSymbols.GetWireIDToWireSymbolsDictionary();
                string leftWireName = wireIDToSymbols.GetValueOrDefault(e.LeftWire)?.WireName ?? "<missing>";
                string rightWireName = wireIDToSymbols.GetValueOrDefault(e.RightWire)?.WireName ?? "<missing>";
                string resultWireName = wireIDToSymbols.GetValueOrDefault(e.ResultWire)?.WireName ?? "<missing>";
                string message = $"{e.Message}\nleft: {leftWireName}\nright: {rightWireName}\nresult: {resultWireName})";
                throw new R1csSumConstraintFailedException(message, e.LeftWire, e.RightWire, e.ResultWire);
            }
            throw;
        }
    }

    public CircuitExecuteResult Compute() {
        if (this.ExecuteResult is not null) {
            throw new Exception("Compute() can be called only once");
        }
        this.ExecuteResult = this.CircuitExecutor.Compute(this.MpcCircuit, this.PublicInputValues, this.PrivateInputValues);
        this.CheckExecuteResult(this.ExecuteResult);
        return this.ExecuteResult;
    }

    protected void CheckExecuteResult(CircuitExecuteResult executeResult) {
        for (int i = 0; i < executeResult.ValueBoard.Count; i++) {
            if (executeResult.ValueBoard[i] is null) {
                throw new Exception($"Wire {i} should not be null");
            }
            if (executeResult.ValueBoard[i]!.IsSecretShare) {
                throw new Exception($"Wire {i} should not be a secret share");
            }
        }
    }

    public Field GetOutput(string wireName) =>
        this.ExecuteResult is null ? throw new Exception("Please call Compute() first") :
        this.ExecuteResult.ValueBoard[this.GetWireID(wireName)]?.AssumeNonShare() ?? throw new Exception($"Output wire {wireName} is null");

    void ICircuitBoardMpcExecutorWrapper.AddPrivate(string wireName, MpcValue value) {
        if (value.IsSecretShare) {
            throw new ArgumentException("MpcValue should not be secret shared for a single party executor", nameof(value));
        }

        this.AddPrivate(wireName, value.Value);
    }
    Task<CircuitExecuteResult> ICircuitBoardMpcExecutorWrapper.Compute() => Task.Run(this.Compute);
    MpcValue ICircuitBoardMpcExecutorWrapper.GetOutput(string wireName) => new(this.GetOutput(wireName), isSecretShare: false);
}
