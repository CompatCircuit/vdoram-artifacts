using SadPencil.CompatCircuitCore.BinarySerialization;
using SadPencil.CompatCircuitCore.Extensions;
using SadPencil.CompatCircuitCore.GlobalConfig;
using System.Buffers.Binary;
using System.Diagnostics;

namespace SadPencil.CompatCircuitCore.CompatCircuits;
public class CompatCircuitOperation : IEquatable<CompatCircuitOperation>, IGeneralBinaryDecodable<CompatCircuitOperation>, IBinaryEncodable {
    public CompatCircuitOperationType OperationType { get; }
    public IReadOnlyList<int> InputWires { get; }
    public IReadOnlyList<int> OutputWires { get; }

    public CompatCircuitOperation(CompatCircuitOperationType operationType, List<int> inputWires, List<int> outputWires) {
        this.OperationType = operationType;
        this.InputWires = inputWires;
        this.OutputWires = outputWires;

        this.CheckWireCount();
    }

    private void CheckWireCount() {
        // Check InputWires
        switch (this.OperationType) {
            case CompatCircuitOperationType.Addition:
            case CompatCircuitOperationType.Multiplication:
                if (this.InputWires.Count != 2) {
                    throw new Exception($"2 input wires are expected, got {this.InputWires.Count}");
                }
                break;
            case CompatCircuitOperationType.Inversion:
            case CompatCircuitOperationType.BitDecomposition:
                if (this.InputWires.Count != 1) {
                    throw new Exception($"Only 1 input wire is expected, got {this.InputWires.Count}");
                }
                break;
            default:
                throw new Exception($"Unknown operation type {this.OperationType}");
        }

        // Check OutputWires
        switch (this.OperationType) {
            case CompatCircuitOperationType.Addition:
            case CompatCircuitOperationType.Multiplication:
            case CompatCircuitOperationType.Inversion:
                if (this.OutputWires.Count != 1) {
                    throw new Exception($"1 output wire is expected, got {this.OutputWires.Count}.");
                }
                break;
            case CompatCircuitOperationType.BitDecomposition:
                if (this.OutputWires.Count != ArithConfig.BitSize) {
                    throw new Exception($"{ArithConfig.BitSize} output wires are expected, got {this.OutputWires.Count}.");
                }
                break;
            default:
                throw new Exception($"Unknown operation type {this.OperationType}");
        }
    }

    public override string ToString() {
        static string ConcatWiresWithComma(IReadOnlyList<int> wires) => wires.Count == 0
                ? "[]"
                : wires.Count == 1 ? $"{wires[0]}" : wires.IsIncreasingByOne() ? $"{wires[0]} .. {wires[^1]}" : string.Join(", ", wires);

        return CompatCircuitOperationTypeHelper.OperationTypeToString(this.OperationType) + " " + this.OperationType switch {
            CompatCircuitOperationType.Addition or CompatCircuitOperationType.Multiplication =>
                ConcatWiresWithComma(this.OutputWires) + " = " + string.Join(this.OperationType == CompatCircuitOperationType.Addition ? " + " : " * ", this.InputWires),
            CompatCircuitOperationType.Inversion or CompatCircuitOperationType.BitDecomposition =>
                ConcatWiresWithComma(this.OutputWires) + " from " + ConcatWiresWithComma(this.InputWires),
            _ => throw new Exception($"Unknown operation type {this.OperationType}"),
        };
    }

    public bool Equals(CompatCircuitOperation? other) => other is not null
&& (ReferenceEquals(this, other)
|| (this.OperationType == other.OperationType
&& this.InputWires.SequenceEqual(other.InputWires) && this.OutputWires.SequenceEqual(other.OutputWires)));

    public override bool Equals(object? obj) => obj is CompatCircuitOperation other && this.Equals(other);

    public override int GetHashCode() {
        HashCode hashCode = new();
        hashCode.Add(this.OperationType);
        foreach (int inputWire in this.InputWires) {
            hashCode.Add(inputWire);
        }
        foreach (int outputWire in this.OutputWires) {
            hashCode.Add(outputWire);
        }
        return hashCode.ToHashCode();
    }

    public int GetEncodedByteCount() => sizeof(byte) + sizeof(int) + (this.InputWires.Count * sizeof(int)) + sizeof(int) + (this.OutputWires.Count * sizeof(int));

    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        bytesWritten = 0;

        destination[bytesWritten..][0] = (byte)this.OperationType;
        bytesWritten += sizeof(byte);

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.InputWires.Count);
        bytesWritten += sizeof(int);

        for (int i = 0; i < this.InputWires.Count; i++) {
            BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.InputWires[i]);
            bytesWritten += sizeof(int);
        }

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.OutputWires.Count);
        bytesWritten += sizeof(int);

        for (int i = 0; i < this.OutputWires.Count; i++) {
            BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.OutputWires[i]);
            bytesWritten += sizeof(int);
        }

        Trace.Assert(bytesWritten == this.GetEncodedByteCount());
    }

    public static CompatCircuitOperation FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead) {
        bytesRead = 0;

        byte operationByte = buffer[bytesRead..][0];
        bytesRead += sizeof(byte);
        CompatCircuitOperationType operationType = (CompatCircuitOperationType)operationByte;

        int inputWiresLength = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        List<int> inputWires = new(inputWiresLength);
        for (int i = 0; i < inputWiresLength; i++) {
            int wire = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
            bytesRead += sizeof(int);
            inputWires.Add(wire);
        }

        int outputWiresLength = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        List<int> outputWires = new(outputWiresLength);
        for (int i = 0; i < outputWiresLength; i++) {
            int wire = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
            bytesRead += sizeof(int);
            outputWires.Add(wire);
        }

        return new CompatCircuitOperation(operationType, inputWires, outputWires);
    }
}
