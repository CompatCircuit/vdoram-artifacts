using SadPencil.CollaborativeZkVm.ZkVmCircuits;
using SadPencil.CollaborativeZkVmExperiment.ExperimentOneCircuits;
using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.CompatCircuits;
using SadPencil.CompatCircuitCore.CompatCircuits.R1csCircuits;
using SadPencil.CompatCircuitCore.Computation;
using SadPencil.CompatCircuitCore.Extensions;
using SadPencil.CompatCircuitCore.GlobalConfig;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives;
using SadPencil.CompatCircuitProgramming.CircuitElements;

namespace SadPencil.CollaborativeZkVmExperiment.ExperimentOneExecutors;
public class ExperimentOneExecutor {
    // TODO: extract ZkProgramExecutor as well as ExperimentExecutor as an interface

    public required IMpcExecutorFactory MpcExecutorFactory { get; init; }
    public required bool IsSingleParty { get; init; }
    public required int MyID { get; init; }

    public required IEnumerator<Field> RandomPublicValueEnumerator { get; init; }

    private Field NextPublicValue() => this.RandomPublicValueEnumerator.MoveNext()
            ? this.RandomPublicValueEnumerator.Current
            : throw new Exception("Insufficient public value");

    private Field NextPrivateValueShare() => ArithConfig.FieldFactory.Random();

    private MpcValue MpcShareFromExistingShare(Field valueShare) =>
        this.IsSingleParty ? new MpcValue(valueShare, isSecretShare: false) : new MpcValue(valueShare, isSecretShare: true);

    private MpcValue MpcShareFromPublicValue(Field value) =>
        this.IsSingleParty ? new(value, isSecretShare: false) : new(this.MyID == 0 ? value : ArithConfig.FieldFactory.Zero, isSecretShare: true);

    public async Task<ExperimentOneExecuteResult> Execute() => await AsyncHelper.TerminateOnException(async () => {
        Serilog.Log.Information($"Compiling AdditionCircuit...");
        CircuitBoard additionCircuitBoard = new AdditionCircuitBoardGenerator().GetCircuitBoard();
        Serilog.Log.Information($"Compiling MultiplicationCircuit...");
        CircuitBoard multiplicationCircuitBoard = new MultiplicationCircuitBoardGenerator().GetCircuitBoard();
        Serilog.Log.Information($"Compiling InversionCircuit...");
        CircuitBoard inversionCircuitBoard = new InversionCircuitBoardGenerator().GetCircuitBoard();
        Serilog.Log.Information($"Compiling BitDecompositionCircuit...");
        CircuitBoard bitDecompositionCircuitBoard = new BitDecompositionCircuitBoardGenerator().GetCircuitBoard();
        Serilog.Log.Information($"Compiling ZkVmExecutorCircuitCircuit...");
        CircuitBoard zkVmCircuitBoard = new ZkVmExecutorCircuitBoardGenerator().GetCircuitBoard().Optimize();

        void FillRandomInputs(CircuitBoardMpcExecutorWrapper circuitExecutorWrapper, CompatCircuitSymbols compatCircuitSymbols) {
            foreach (CompatCircuitWireSymbol symbol in compatCircuitSymbols.CircuitWireSymbols) {
                if (symbol.IsPublicInput) {
                    circuitExecutorWrapper.AddPublic(symbol.WireName, this.NextPublicValue());
                }
                else if (symbol.IsPrivateInput) {
                    circuitExecutorWrapper.AddPrivate(symbol.WireName, this.MpcShareFromExistingShare(this.NextPrivateValueShare()));
                }
            }
        }

        // TODO: require an Action<string, R1csCircuitWithValues>. Don't save r1cs here, reducing RAM usage.
        Dictionary<string, (CircuitExecuteResult CircuitExecuteResult, R1csCircuitWithValues R1csCircuitWithValues)> results = [];
        async Task RunCircuitBoardWithRandomInputs(CircuitBoard circuitBoard, string circuitName, bool saveResult = true) => await AsyncHelper.TerminateOnException(async () => {
            Serilog.Log.Information($"Executing {circuitName}...");
            CircuitBoardMpcExecutorWrapper executorWrapper;
            CircuitBoardConverter.ToCompatCircuit(circuitBoard, circuitName, out CompatCircuit compatCircuit, out CompatCircuitSymbols compatCircuitSymbols);
            executorWrapper = new CircuitBoardMpcExecutorWrapper(compatCircuit, compatCircuitSymbols, this.MpcExecutorFactory.NextExecutor());
            FillRandomInputs(executorWrapper, compatCircuitSymbols);
            CircuitExecuteResult result = await executorWrapper.Compute();
            if (saveResult) {
                results.Add(circuitName, (result, executorWrapper.GetR1csCircuitWithValues()));
            }
        });

        // Warm up. Not counted
        await RunCircuitBoardWithRandomInputs(additionCircuitBoard, "WarmUp", saveResult: false);

        await RunCircuitBoardWithRandomInputs(additionCircuitBoard, $"Addition-{AdditionCircuitBoardGenerator.RepeatCount}");
        await RunCircuitBoardWithRandomInputs(multiplicationCircuitBoard, $"Multiplication-{MultiplicationCircuitBoardGenerator.RepeatCount}");
        await RunCircuitBoardWithRandomInputs(inversionCircuitBoard, $"Inversion-{InversionCircuitBoardGenerator.RepeatCount}");
        await RunCircuitBoardWithRandomInputs(bitDecompositionCircuitBoard, $"BitDecomposition-{BitDecompositionCircuitBoardGenerator.RepeatCount}");
        await RunCircuitBoardWithRandomInputs(zkVmCircuitBoard, "zkVM-IE");

        return new ExperimentOneExecuteResult() {
            TotalTime = results.Values.Select(v => v.CircuitExecuteResult.TotalTime).Aggregate(TimeSpan.Zero, (total, next) => total + next),
            StepTimes = results.Select(kvp => (kvp.Key, kvp.Value.CircuitExecuteResult.TotalTime)).ToDictionary(),
            R1csCircuitsWithValues = results.Select(kvp => (kvp.Key, kvp.Value.R1csCircuitWithValues)).ToDictionary(),
        };
    });
}
