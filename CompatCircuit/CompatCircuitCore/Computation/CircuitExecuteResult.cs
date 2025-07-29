using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.CompatCircuits.MpcCircuits;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives;

namespace Anonymous.CompatCircuitCore.Computation;
public class CircuitExecuteResult {
    public required MpcCircuit MpcCircuit { get; init; }
    public required List<MpcValue?> ValueBoard { get; init; }

    private Dictionary<int, Field>? _publicOutputs = null;
    public Dictionary<int, Field> PublicOutputs => this._publicOutputs ??= this.GetPublicOutputs();

    public required TimeSpan TotalTime { get; init; }

    public CircuitExecuteResult() { }

    private Dictionary<int, Field> GetPublicOutputs() {
        Dictionary<int, Field> publicOutputs = [];
        foreach (int wireID in this.MpcCircuit.PublicOutputs) {
            MpcValue value = this.ValueBoard[wireID] ?? throw new Exception($"Public output wire {wireID} should not be null. The circuit might be incorrect.");
            if (value.IsSecretShare) {
                throw new Exception($"Public output wire {wireID} is a secret share. This should not happen. Executor might be incorrectly implemented.");
            }
            publicOutputs.Add(wireID, this.ValueBoard[wireID]!.Value);
        }
        return publicOutputs;
    }
}
