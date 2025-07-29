using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.BinarySerialization;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
public record FieldBeaverTripleShare : IBinaryEncodable, IArithFactoryBinaryDecodable<FieldBeaverTripleShare, Field> {
    public required Field X { get; init; }
    public required Field Y { get; init; }
    public required Field XY { get; init; }

    public List<Field> ToList() => [this.X, this.Y, this.XY];
    public Field[] ToArray() => [this.X, this.Y, this.XY];
    public void Deconstruct(out Field x, out Field y, out Field xy) => (x, y, xy) = (this.X, this.Y, this.XY);

    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        bytesWritten = 0;

        foreach (Field value in this.ToList()) {
            value.EncodeBytes(destination[bytesWritten..], out int valueBytesWritten);
            bytesWritten += valueBytesWritten;
        }
        Trace.Assert(bytesWritten == this.GetEncodedByteCount());
    }

    public int GetEncodedByteCount() => this.X.GetEncodedByteCount() + this.Y.GetEncodedByteCount() + this.XY.GetEncodedByteCount();
    public static FieldBeaverTripleShare FromEncodedBytes(ReadOnlySpan<byte> buffer, IArithFactory<Field> factory, out int bytesRead) {
        bytesRead = 0;

        Field x = Field.FromEncodedBytes(buffer[bytesRead..], factory, out int xBytesRead);
        bytesRead += xBytesRead;

        Field y = Field.FromEncodedBytes(buffer[bytesRead..], factory, out int yBytesRead);
        bytesRead += yBytesRead;

        Field xy = Field.FromEncodedBytes(buffer[bytesRead..], factory, out int xyBytesRead);
        bytesRead += xyBytesRead;

        return new FieldBeaverTripleShare() { X = x, Y = y, XY = xy };
    }
}
