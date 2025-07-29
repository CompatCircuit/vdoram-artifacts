using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.BinarySerialization;
using Anonymous.CompatCircuitCore.GlobalConfig;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Anonymous.CompatCircuitCore.CompatCircuits;
public class CompatCircuit : IEquatable<CompatCircuit>, IArithFactoryBinaryDecodable<CompatCircuit, Field>, IBinaryEncodable {
    /// <summary>
    /// Wires whose ID is between [0, ReservedWireConstantCount) are reserved: 
    /// Wire 0 for 0.
    /// Wire 1 for -1.
    /// Wire 2 for 1 (2^0).
    /// Wire 3 for 2 (2^1).
    /// Wire 4 for 2^2.
    /// Wire 5 for 2^3.
    /// Wire 2 + i for 2^i.
    /// Wire 1 + FieldBitSize for 2^{FieldBitSize-1}.
    /// Wire 2 + FieldBitSize for FieldQuadraticNonresidue
    /// Wire 3 + FieldBitSize for -FieldQuadraticNonresidue
    /// </summary>
    public static int ReservedWireConstantCount { get; } = 4 + ArithConfig.BitSize;

    /// <summary>
    /// Wires whose ID is between [ReservedWireCount, ConstantWireCount) are considered constant, whose values are specified in ConstantInputs.
    /// </summary>
    public required int ConstantWireCount { get; init; }

    /// <summary>
    /// Wires whose ID is between [ConstantWireCount, PublicInputWireCount) are considered public input wires, whose values are given in plaintext.
    /// </summary>
    public required int PublicInputWireCount { get; init; }

    /// <summary>
    /// Wires whose ID is between [PublicInputWireCount, InputWireCount) are considered private input wires. Each value will be given in secret sharing shares.
    /// </summary>
    public required int InputWireCount { get; init; }

    /// <summary>
    /// The number of wires. Also, wires whose ID is between [InputWireCount, WireCount) are computed from Operations.
    /// </summary>
    public required int WireCount { get; init; }

    /// <summary>
    /// Contains constant input values for wires whose ID is between [ReservedWireCount, ConstantWireCount). The i-th element in ConstantInputs corresponds to Wire i+ReservedWireCount.
    /// </summary>
    public required IReadOnlyList<Field> ConstantInputs { get; init; }

    /// <summary>
    /// Contains IDs of those wires that should be exposed as public values.
    /// </summary>
    public required IReadOnlySet<int> PublicOutputs { get; init; }

    private IReadOnlyList<CompatCircuitOperation>? _operations = null;
    /// <summary>
    /// Operations that compute output wires based on input wires. Must be given in topological order.
    /// </summary>
    public required IReadOnlyList<CompatCircuitOperation> Operations {
        get {
            Trace.Assert(this._operations is not null);
            return this._operations;
        }
        init {
            foreach (CompatCircuitOperation operation in value) {
                if (!this.AllowedOperationTypes.Contains(operation.OperationType)) {
                    throw new Exception($"Operation type {operation.OperationType} is not allowed.");
                }
            }

            this._operations = value;
        }
    }

    private readonly ImmutableSortedSet<CompatCircuitOperationType> _allowedOperationTypes = Enum.GetValues<CompatCircuitOperationType>().ToImmutableSortedSet();
    protected virtual ImmutableSortedSet<CompatCircuitOperationType> AllowedOperationTypes => this._allowedOperationTypes;

    public CompatCircuit() { }

    [SetsRequiredMembers]
    public CompatCircuit(CompatCircuit circuit) {
        this.ConstantWireCount = circuit.ConstantWireCount;
        this.PublicInputWireCount = circuit.PublicInputWireCount;
        this.InputWireCount = circuit.InputWireCount;
        this.WireCount = circuit.WireCount;
        this.ConstantInputs = circuit.ConstantInputs;
        this.PublicOutputs = circuit.PublicOutputs;
        this.Operations = circuit.Operations;
    }

    public static IReadOnlyList<Field> ReservedWireValues { get; } = GetReservedWireValues();

    private static List<Field> GetReservedWireValues() {
        // Wire 0 for 0.
        // Wire 1 for -1.
        // Wire 2 for 1 (2^0).
        // Wire 3 for 2 (2^1).
        List<Field> outputs = [
            ArithConfig.FieldFactory.Zero,
            ArithConfig.FieldFactory.NegOne,
            ArithConfig.FieldFactory.One,
        ];

        // Wire 4 for 2^2.
        // Wire 5 for 2^3.
        // Wire 2 + i for 2^i.
        // Wire 1 + FieldBitSize for 2^{FieldBitSize-1}.
        BigInteger twoPowers = 1;
        for (int i = 1; i < ArithConfig.BitSize; i++) {
            twoPowers *= 2;
            outputs.Add(ArithConfig.FieldFactory.New(twoPowers));
        }

        outputs.Add(ArithConfig.FieldFactory.New(ArithConfig.FieldQuadraticNonresidue));
        outputs.Add(-ArithConfig.FieldFactory.New(ArithConfig.FieldQuadraticNonresidue));

        Trace.Assert(outputs.Count == ReservedWireConstantCount);
        return outputs;
    }

    public bool Equals(CompatCircuit? other) => other is not null
&& (ReferenceEquals(this, other)
|| (this.ConstantWireCount == other.ConstantWireCount
&& this.PublicInputWireCount == other.PublicInputWireCount
&& this.InputWireCount == other.InputWireCount
&& this.WireCount == other.WireCount
&& this.ConstantInputs.SequenceEqual(other.ConstantInputs)
&& this.PublicOutputs.SetEquals(other.PublicOutputs) && this.Operations.SequenceEqual(other.Operations)));

    public override bool Equals(object? obj) => obj is CompatCircuit other && this.Equals(other);

    public override int GetHashCode() {
        HashCode hashCode = new();
        hashCode.Add(this.ConstantWireCount);
        hashCode.Add(this.PublicInputWireCount);
        hashCode.Add(this.InputWireCount);
        hashCode.Add(this.WireCount);
        foreach (Field constantInput in this.ConstantInputs) {
            hashCode.Add(constantInput);
        }
        foreach (int publicOutput in this.PublicOutputs) {
            hashCode.Add(publicOutput);
        }
        foreach (CompatCircuitOperation operation in this.Operations) {
            hashCode.Add(operation);
        }
        return hashCode.ToHashCode();
    }

    public int GetEncodedByteCount() => 0
        + sizeof(int)
        + sizeof(int)
        + sizeof(int)
        + sizeof(int)
        + sizeof(int)
        + this.ConstantInputs.Select(v => v.GetEncodedByteCount()).Sum()
        + sizeof(int)
        + (this.PublicOutputs.Count * sizeof(int))
        + sizeof(int)
        + this.Operations.Select(v => v.GetEncodedByteCount()).Sum();

    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        bytesWritten = 0;

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.ConstantWireCount);
        bytesWritten += sizeof(int);

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.PublicInputWireCount);
        bytesWritten += sizeof(int);

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.InputWireCount);
        bytesWritten += sizeof(int);

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.WireCount);
        bytesWritten += sizeof(int);

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.ConstantInputs.Count);
        bytesWritten += sizeof(int);

        foreach (Field constantInput in this.ConstantInputs) {
            constantInput.EncodeBytes(destination[bytesWritten..], out int constantInputBytesWritten);
            bytesWritten += constantInputBytesWritten;
        }

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.PublicOutputs.Count);
        bytesWritten += sizeof(int);

        foreach (int publicOutput in this.PublicOutputs) {
            BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], publicOutput);
            bytesWritten += sizeof(int);
        }

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.Operations.Count);
        bytesWritten += sizeof(int);

        foreach (CompatCircuitOperation operation in this.Operations) {
            operation.EncodeBytes(destination[bytesWritten..], out int operationBytesWritten);
            bytesWritten += operationBytesWritten;
        }

        Trace.Assert(bytesWritten == this.GetEncodedByteCount());
    }

    public static CompatCircuit FromEncodedBytes(ReadOnlySpan<byte> buffer, IArithFactory<Field> factory, out int bytesRead) {
        bytesRead = 0;

        int constantWireCount = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        int publicInputWireCount = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        int inputWireCount = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        int wireCount = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        int constantInputCount = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        List<Field> constantInputs = [];
        for (int i = 0; i < constantInputCount; i++) {
            Field constantInput = Field.FromEncodedBytes(buffer[bytesRead..], factory, out int constantInputBytesRead);
            bytesRead += constantInputBytesRead;
            constantInputs.Add(constantInput);
        }

        int publicOutputCount = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        HashSet<int> publicOutputs = [];
        for (int i = 0; i < publicOutputCount; i++) {
            int publicOutput = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
            bytesRead += sizeof(int);
            bool notExistedBefore = publicOutputs.Add(publicOutput);
            if (!notExistedBefore) {
                throw new Exception("Duplicated public output wire ID.");
            }
        }

        int operationCount = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        List<CompatCircuitOperation> operations = [];
        for (int i = 0; i < operationCount; i++) {
            CompatCircuitOperation operation = CompatCircuitOperation.FromEncodedBytes(buffer[bytesRead..], out int operationBytesRead);
            bytesRead += operationBytesRead;
            operations.Add(operation);
        }

        return new CompatCircuit {
            ConstantWireCount = constantWireCount,
            PublicInputWireCount = publicInputWireCount,
            InputWireCount = inputWireCount,
            WireCount = wireCount,
            ConstantInputs = constantInputs,
            PublicOutputs = publicOutputs,
            Operations = operations,
        };
    }
}
