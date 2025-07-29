using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Anonymous.CompatCircuitCore.CompatCircuits.BasicCircuits;
public class BasicCircuit : CompatCircuit {
    private readonly ImmutableSortedSet<CompatCircuitOperationType> _allowedOperationTypes = [CompatCircuitOperationType.Addition, CompatCircuitOperationType.Multiplication];
    protected override ImmutableSortedSet<CompatCircuitOperationType> AllowedOperationTypes => this._allowedOperationTypes;

    public BasicCircuit() { }

    [SetsRequiredMembers]
    public BasicCircuit(CompatCircuit compatCircuit) : base(compatCircuit) { }
}
