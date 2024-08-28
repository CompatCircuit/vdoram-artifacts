using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.BinarySerialization;
using SadPencil.CompatCircuitCore.Extensions;
using System.Collections;
using System.Diagnostics;

namespace SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.EdaBitsKai;
public class EdaBitsKaiShare : IBinaryEncodable, IArithFactoryBinaryDecodable<EdaBitsKaiShare, Field>, IEquatable<EdaBitsKaiShare> {
    public required Field ArithShare { get; init; }
    public required IReadOnlyList<bool> BoolShares { get; init; }

    public static EdaBitsKaiShare FromEncodedBytes(ReadOnlySpan<byte> buffer, IArithFactory<Field> factory, out int bytesRead) {
        bytesRead = 0;

        Field arithShare = Field.FromEncodedBytes(buffer[bytesRead..], factory, out int arithBytesRead);
        bytesRead += arithBytesRead;

        BitArray bits = BitArrayHelper.FromEncodedBytes(buffer[bytesRead..], out int bitsBytesRead);
        bytesRead += bitsBytesRead;

        return new EdaBitsKaiShare() { ArithShare = arithShare, BoolShares = bits.ToList() };
    }

    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        bytesWritten = 0;

        this.ArithShare.EncodeBytes(destination[bytesWritten..], out int arithBytesWritten);
        bytesWritten += arithBytesWritten;

        BitArray bits = new(this.BoolShares.ToArray());
        bits.EncodeBytes(destination[bytesWritten..], out int bitsBytesWritten);
        bytesWritten += bitsBytesWritten;

        Trace.Assert(bytesWritten == this.GetEncodedByteCount());
    }

    public int GetEncodedByteCount() => this.ArithShare.GetEncodedByteCount() + BitArrayHelper.GetEncodedByteCount(this.BoolShares);

    public bool Equals(EdaBitsKaiShare? other) => other is not null && this.ArithShare == other.ArithShare && this.BoolShares.SequenceEqual(other.BoolShares);
    public override bool Equals(object? obj) => this.Equals(obj as EdaBitsKaiShare);
    public override int GetHashCode() {
        // https://stackoverflow.com/a/56539595
        HashCode hashCode = new();
        hashCode.Add(this.ArithShare);
        foreach (bool boolShare in this.BoolShares) {
            hashCode.Add(boolShare);
        }
        return hashCode.ToHashCode();
    }
}
