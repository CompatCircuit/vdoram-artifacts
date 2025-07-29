using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.CompatCircuits;
using Anonymous.CompatCircuitCore.CompatCircuits.MpcCircuits;
using Anonymous.CompatCircuitCore.CompatCircuits.R1csCircuits;
using Anonymous.CompatCircuitCore.Computation;

namespace Anonymous.CompatCircuitProgramming.CircuitElements;
public abstract class CircuitBoardExecutorWrapperBase<TPrivateInputValue> {
    protected CompatCircuit CompatCircuit { get; }
    protected CompatCircuitSymbols CompatCircuitSymbols { get; }

    protected MpcCircuit MpcCircuit { get; }
    protected R1csCircuit R1csCircuit { get; }
    protected IReadOnlyDictionary<string, CompatCircuitWireSymbol> WireNameToWireSymbols { get; }

    protected int GetWireID(string wireName) => this.WireNameToWireSymbols[wireName].WireID;

    protected Dictionary<int, Field> PublicInputValues { get; } = [];
    protected Dictionary<int, TPrivateInputValue> PrivateInputValues { get; } = [];

    public void AddPublic(string wireName, Field value) => this.PublicInputValues.Add(this.GetWireID(wireName), value);
    public void AddPrivate(string wireName, TPrivateInputValue value) => this.PrivateInputValues.Add(this.GetWireID(wireName), value);

    protected CircuitExecuteResult? ExecuteResult { get; set; } = null;
    public R1csCircuitWithValues GetR1csCircuitWithValues() => this.ExecuteResult is null ? throw new Exception("Please call Compute() first") : R1csCircuitWithValues.FromR1csCircuit(this.R1csCircuit, wireValues: this.ExecuteResult!.ValueBoard);

    protected CircuitBoardExecutorWrapperBase(CompatCircuit compatCircuit, CompatCircuitSymbols compatCircuitSymbols) {
        this.CompatCircuit = compatCircuit;
        this.CompatCircuitSymbols = compatCircuitSymbols;

        this.WireNameToWireSymbols = compatCircuitSymbols.GetWireNameToWireSymbolsDictionary();

        CompatCircuitConverter.ToMpcCircuitAndR1csCircuit(compatCircuit, out MpcCircuit mpcCircuit, out R1csCircuit r1csCircuit);
        this.MpcCircuit = mpcCircuit;
        this.R1csCircuit = r1csCircuit;
    }
}
