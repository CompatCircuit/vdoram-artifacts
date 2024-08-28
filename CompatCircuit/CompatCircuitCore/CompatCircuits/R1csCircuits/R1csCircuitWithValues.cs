using SadPencil.CompatCircuitCore.CompatCircuits.R1csCircuits.Exceptions;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives;

namespace SadPencil.CompatCircuitCore.CompatCircuits.R1csCircuits;
public class R1csCircuitWithValues : R1csCircuit {
    public required IReadOnlyList<MpcValue> WireValues { get; init; }

    public R1csCircuitWithValues() : base() { }

    public static R1csCircuitWithValues FromR1csCircuit(R1csCircuit r1csCircuit, IReadOnlyList<MpcValue> wireValues) => new() {
        WireCount = r1csCircuit.WireCount,
        PublicWireCount = r1csCircuit.PublicWireCount,
        ProductConstraints = r1csCircuit.ProductConstraints,
        SumConstraints = r1csCircuit.SumConstraints,
        WireValues = wireValues,
    };

    public void SelfVerify() {
        if (this.WireValues.Any(value => value.IsSecretShare)) {
            throw new InvalidOperationException("This method is only possible when all values are not secret shares.");
        }

        foreach ((int left, int right, int result) in this.ProductConstraints) {
            if (this.WireValues[left].Value * this.WireValues[right].Value != this.WireValues[result].Value) {
                throw new R1csProductConstraintFailedException($"Product constraint failed", left, right, result);
            }
        }

        foreach ((int left, int right, int result) in this.SumConstraints) {
            if (this.WireValues[left].Value + this.WireValues[right].Value != this.WireValues[result].Value) {
                throw new R1csSumConstraintFailedException($"Sum constraint failed", left, right, result);
            }
        }
    }
}
