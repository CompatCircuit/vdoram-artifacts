using Anonymous.CompatCircuitCore.Arithmetic;
using System.Diagnostics.CodeAnalysis;

namespace Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives;
public record MpcValue {
    public required Field Value { get; init; }
    public required bool IsSecretShare { get; init; }
    public MpcValue() { }

    [SetsRequiredMembers]
    public MpcValue(Field value, bool isSecretShare) {
        this.Value = value;
        this.IsSecretShare = isSecretShare;
    }

    public Field AssumeNonShare() => this.IsSecretShare ? throw new Exception("This MpcValue is a secret share") : this.Value;
}
