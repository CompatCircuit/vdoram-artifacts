using System.Diagnostics.CodeAnalysis;

namespace Anonymous.CompatCircuitCore.CompatCircuits.MpcCircuits;
public class MpcCircuit : CompatCircuit {
    public MpcCircuit() { }

    /// <summary>
    /// Note: you should use CompatCircuitConverter to convert CompatCircuit to MpcCircuit. This initializer does not add verification patterns and is meant for internal use.
    /// </summary>
    /// <param name="compatCircuit"></param>
    [SetsRequiredMembers]
    public MpcCircuit(CompatCircuit compatCircuit) {
        this.ConstantInputs = compatCircuit.ConstantInputs;
        this.ConstantWireCount = compatCircuit.ConstantWireCount;
        this.PublicInputWireCount = compatCircuit.PublicInputWireCount;
        this.InputWireCount = compatCircuit.InputWireCount;
        this.WireCount = compatCircuit.WireCount;
        this.PublicOutputs = compatCircuit.PublicOutputs;
        this.Operations = compatCircuit.Operations;
    }
}
