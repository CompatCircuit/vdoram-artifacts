using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.CompatCircuits.BasicCircuits;
using SadPencil.CompatCircuitCore.CompatCircuits.MpcCircuits;
using SadPencil.CompatCircuitCore.CompatCircuits.R1csCircuits;
using SadPencil.CompatCircuitCore.Extensions;
using SadPencil.CompatCircuitCore.GlobalConfig;
using SadPencil.CompatCircuitCore.PrecompiledCircuits;
using System.Collections.Immutable;
using System.Diagnostics;

namespace SadPencil.CompatCircuitCore.CompatCircuits;
public static class CompatCircuitConverter {
    private static CompatCircuit AddInversionProof(CompatCircuit compatCircuit, out List<int> publicZeroOutputWires) {
        int constantWireCount = compatCircuit.ConstantWireCount;
        int publicInputWireCount = compatCircuit.PublicInputWireCount;
        int inputWireCount = compatCircuit.InputWireCount;
        int wireCount = compatCircuit.WireCount;
        IReadOnlyList<Field> constantInputs = compatCircuit.ConstantInputs;
        IReadOnlySet<int> publicOutputs = compatCircuit.PublicOutputs;
        List<CompatCircuitOperation> operations = compatCircuit.Operations.ToList();

        publicZeroOutputWires = [];

        int NextWire() => wireCount++;

        int negOneWire = CompatCircuit.ReservedWireValues.IndexOf(ArithConfig.FieldFactory.NegOne);
        Trace.Assert(negOneWire >= 0);

        foreach (CompatCircuitOperation op in compatCircuit.Operations.Where(op => op.OperationType == CompatCircuitOperationType.Inversion)) {
            Trace.Assert(op.InputWires.Count == 1 && op.OutputWires.Count == 1);

            // (1) in * (in * out - 1) = 0
            // (2) out * (in * out - 1) = 0

            int inWire = op.InputWires[0];
            int outWire = op.OutputWires[0];

            int inTimesOutWire = NextWire();
            operations.Add(new CompatCircuitOperation(CompatCircuitOperationType.Multiplication, [inWire, outWire], [inTimesOutWire]));

            int inTimesOutMinusOneWire = NextWire();
            operations.Add(new CompatCircuitOperation(CompatCircuitOperationType.Addition, [inTimesOutWire, negOneWire], [inTimesOutMinusOneWire]));

            int inTimesInTimesOutMinusOneWire = NextWire();
            operations.Add(new CompatCircuitOperation(CompatCircuitOperationType.Multiplication, [inWire, inTimesOutMinusOneWire], [inTimesInTimesOutMinusOneWire]));
            publicZeroOutputWires.Add(inTimesInTimesOutMinusOneWire);

            int outTimesInTimesOutMinusOneWire = NextWire();
            operations.Add(new CompatCircuitOperation(CompatCircuitOperationType.Multiplication, [outWire, inTimesOutMinusOneWire], [outTimesInTimesOutMinusOneWire]));
            publicZeroOutputWires.Add(outTimesInTimesOutMinusOneWire);
        }

        return new CompatCircuit {
            ConstantInputs = constantInputs,
            ConstantWireCount = constantWireCount,
            PublicInputWireCount = publicInputWireCount,
            InputWireCount = inputWireCount,
            WireCount = wireCount,
            PublicOutputs = publicOutputs,
            Operations = operations,
        };
    }

    private static CompatCircuit AddBitDecompositionProof(CompatCircuit compatCircuit, out List<int> publicZeroOutputWires) {
        BasicCircuit bitDecompositionProofCircuit = BitDecompositionProof.Circuit;
        CompatCircuitSymbols bitDecompositionProofCircuitSymbols = BitDecompositionProof.CompatCircuitSymbols;

        if (bitDecompositionProofCircuit.ConstantWireCount - CompatCircuit.ReservedWireConstantCount != 0) {
            // Note: if you have modified the bitDecompositionProofCircuit, you may need to update this check and write additional code to handle the case where the circuit has constant wires, i.e., inserting them and update all existing wire IDs
            throw new Exception("BitDecompositionProofCircuit should not have constant wires");
        }
        if (bitDecompositionProofCircuit.PublicInputWireCount - bitDecompositionProofCircuit.ConstantWireCount != 0) {
            throw new Exception("BitDecompositionProofCircuit should not have public input wires");
        }

        // Input mapping
        int inputFieldWireInBitDecompositionProofCircuit;
        ImmutableSortedSet<int> inputBitWiresInBitDecompositionProofCircuit;
        {
            Dictionary<string, CompatCircuitWireSymbol> wireNameToWireSymbols = bitDecompositionProofCircuitSymbols.GetWireNameToWireSymbolsDictionary();
            inputFieldWireInBitDecompositionProofCircuit = wireNameToWireSymbols["input_field"].WireID;
            inputBitWiresInBitDecompositionProofCircuit = Enumerable.Range(0, ArithConfig.BitSize)
                .Select(i => $"input_bit_{i}")
                .Select(name => wireNameToWireSymbols[name].WireID)
                .ToImmutableSortedSet();
            Trace.Assert(inputBitWiresInBitDecompositionProofCircuit.Count == ArithConfig.BitSize);
        }

        int constantWireCount = compatCircuit.ConstantWireCount;
        int publicInputWireCount = compatCircuit.PublicInputWireCount;
        int inputWireCount = compatCircuit.InputWireCount;
        int wireCount = compatCircuit.WireCount;
        IReadOnlyList<Field> constantInputs = compatCircuit.ConstantInputs;
        IReadOnlySet<int> publicOutputs = compatCircuit.PublicOutputs;
        List<CompatCircuitOperation> operations = compatCircuit.Operations.ToList();

        publicZeroOutputWires = [];

        List<CompatCircuitOperation> bitDecompositionOps = compatCircuit.Operations.Where(op => op.OperationType == CompatCircuitOperationType.BitDecomposition).ToList();
        foreach (CompatCircuitOperation bitDecompositionOp in bitDecompositionOps) {
            Trace.Assert(bitDecompositionOp.InputWires.Count == 1);
            Trace.Assert(1 + bitDecompositionOp.OutputWires.Count == bitDecompositionProofCircuit.InputWireCount - bitDecompositionProofCircuit.PublicInputWireCount);

            foreach (CompatCircuitOperation op in bitDecompositionProofCircuit.Operations) {
                Trace.Assert(op.OperationType is CompatCircuitOperationType.Addition or CompatCircuitOperationType.Multiplication);
                Trace.Assert(op.InputWires.Count == 2);
                Trace.Assert(op.OutputWires.Count == 1);
                Trace.Assert(op.OutputWires[0] >= bitDecompositionProofCircuit.InputWireCount);

                int MapWireID(int wireIDInBitDecompositionProofCircuit) {
                    if (wireIDInBitDecompositionProofCircuit < CompatCircuit.ReservedWireConstantCount) {
                        return wireIDInBitDecompositionProofCircuit;
                    }
                    else if (wireIDInBitDecompositionProofCircuit < bitDecompositionProofCircuit.InputWireCount) {
                        if (wireIDInBitDecompositionProofCircuit == inputFieldWireInBitDecompositionProofCircuit) {
                            return bitDecompositionOp.InputWires[0];
                        }
                        int bitIndex = inputBitWiresInBitDecompositionProofCircuit.IndexOf(wireIDInBitDecompositionProofCircuit);
                        return bitIndex >= 0
                            ? bitDecompositionOp.OutputWires[bitIndex]
                            : throw new Exception($"Unexpected wire ID {wireIDInBitDecompositionProofCircuit}");
                    }
                    else {
                        // Note: this method captures variable "wireCount". This is intended.
                        return wireCount + wireIDInBitDecompositionProofCircuit - bitDecompositionProofCircuit.InputWireCount;
                    }
                }

                List<int> newInputWires = op.InputWires.Select(MapWireID).ToList();
                List<int> newOutputWires = op.OutputWires.Select(MapWireID).ToList();
                operations.Add(new CompatCircuitOperation(op.OperationType, newInputWires, newOutputWires));

                // Add to publicZeroOutputWires if the output wire is a public output
                // Note: all public outputs in bitDecompositionProofCircuit should be zero. This is required for the bit decomposition proof.
                int oldOutputWire = op.OutputWires[0];
                int newOutputWire = newOutputWires[0];
                if (bitDecompositionProofCircuit.PublicOutputs.Contains(oldOutputWire)) {
                    publicZeroOutputWires.Add(newOutputWire);
                }
            }

            wireCount += bitDecompositionProofCircuit.WireCount - bitDecompositionProofCircuit.InputWireCount;
        }

        return new CompatCircuit {
            ConstantInputs = constantInputs,
            ConstantWireCount = constantWireCount,
            PublicInputWireCount = publicInputWireCount,
            InputWireCount = inputWireCount,
            WireCount = wireCount,
            PublicOutputs = publicOutputs,
            Operations = operations,
        };
    }

    public static MpcCircuit ToMpcCircuit(CompatCircuit compatCircuit) {
        List<int> publicZeroOutputWires = [];
        {
            compatCircuit = AddInversionProof(compatCircuit, out List<int> newPublicZeroOutputWires);
            publicZeroOutputWires.AddRange(newPublicZeroOutputWires);
        }
        {
            compatCircuit = AddBitDecompositionProof(compatCircuit, out List<int> newPublicZeroOutputWires);
            publicZeroOutputWires.AddRange(newPublicZeroOutputWires);
        }

        // Convert to MpcCircuit
        return new MpcCircuit(compatCircuit);
    }

    public static void ToMpcCircuitAndR1csCircuit(CompatCircuit compatCircuit, out MpcCircuit mpcCircuit, out R1csCircuit r1CsCircuit) {
        List<int> publicZeroOutputWires = [];
        {
            compatCircuit = AddInversionProof(compatCircuit, out List<int> newPublicZeroOutputWires);
            publicZeroOutputWires.AddRange(newPublicZeroOutputWires);
        }
        {
            compatCircuit = AddBitDecompositionProof(compatCircuit, out List<int> newPublicZeroOutputWires);
            publicZeroOutputWires.AddRange(newPublicZeroOutputWires);
        }

        // Convert to MpcCircuit
        mpcCircuit = new MpcCircuit(compatCircuit);

        // Convert to R1csCircuit
        int wireCount = compatCircuit.WireCount;
        int publicWireCount = compatCircuit.PublicInputWireCount;

        IReadOnlyList<Field> reservedWireValues = CompatCircuit.ReservedWireValues;
        int constant0WireID = reservedWireValues.IndexOf(ArithConfig.FieldFactory.Zero);
        int constant1WireID = reservedWireValues.IndexOf(ArithConfig.FieldFactory.One);

        List<R1csConstraint> productConstraints = [];
        List<R1csConstraint> sumConstraints = [];

        foreach (CompatCircuitOperation operation in compatCircuit.Operations) {
            switch (operation.OperationType) {
                case CompatCircuitOperationType.Addition: {
                        Trace.Assert(operation.InputWires.Count == 2 && operation.OutputWires.Count == 1);
                        sumConstraints.Add(new R1csConstraint(operation.InputWires[0], operation.InputWires[1], operation.OutputWires[0]));
                    }
                    break;
                case CompatCircuitOperationType.Multiplication: {
                        Trace.Assert(operation.InputWires.Count == 2 && operation.OutputWires.Count == 1);
                        productConstraints.Add(new R1csConstraint(operation.InputWires[0], operation.InputWires[1], operation.OutputWires[0]));
                    }
                    break;
                case CompatCircuitOperationType.Inversion:
                    // Do nothing. This is because we already added the inversion proof.
                    break;
                case CompatCircuitOperationType.BitDecomposition:
                    // Do nothing. This is because we already added the bit decomposition proof.
                    break;
                default:
                    throw new Exception($"Unknown operation type {operation.OperationType}");
            }
        }

        // Add additional zero checks for inversion proof and bit decomposition proof
        foreach (int publicZeroOutputWire in publicZeroOutputWires) {
            productConstraints.Add(new R1csConstraint(publicZeroOutputWire, constant1WireID, constant0WireID));
        }

        // That's it
        r1CsCircuit = new R1csCircuit { ProductConstraints = productConstraints, SumConstraints = sumConstraints, WireCount = wireCount, PublicWireCount = publicWireCount };

        return;
    }
}