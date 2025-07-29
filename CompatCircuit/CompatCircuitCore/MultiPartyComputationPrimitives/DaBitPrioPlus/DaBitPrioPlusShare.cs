using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.BinarySerialization;

namespace Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.DaBitPrioPlus;
public class DaBitPrioPlusShare : IBinaryEncodable, IArithFactoryBinaryDecodable<DaBitPrioPlusShare, Field>, IEquatable<DaBitPrioPlusShare> {
    public required Field ArithShare { get; init; }
    public required bool BoolShare { get; init; }

    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        bytesWritten = 0;

        this.ArithShare.EncodeBytes(destination[bytesWritten..], out int arithBytesWritten);
        bytesWritten += arithBytesWritten;

        destination[bytesWritten..][0] = this.BoolShare ? (byte)1 : (byte)0;
        bytesWritten += 1;
    }

    public int GetEncodedByteCount() => this.ArithShare.GetEncodedByteCount() + 1;

    public static DaBitPrioPlusShare FromEncodedBytes(ReadOnlySpan<byte> buffer, IArithFactory<Field> factory, out int bytesRead) {
        bytesRead = 0;

        Field arithShare = Field.FromEncodedBytes(buffer[bytesRead..], factory, out int arithBytesRead);
        bytesRead += arithBytesRead;

        bool boolShare = buffer[bytesRead] == 1;
        bytesRead += 1;

        return new DaBitPrioPlusShare() { ArithShare = arithShare, BoolShare = boolShare };
    }

    public bool Equals(DaBitPrioPlusShare? other) => other is not null && this.ArithShare == other.ArithShare && this.BoolShare == other.BoolShare;
    public override bool Equals(object? obj) => this.Equals(obj as DaBitPrioPlusShare);
    public override int GetHashCode() => (this.ArithShare, this.BoolShare).GetHashCode();
}
