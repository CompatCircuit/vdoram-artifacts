using Anonymous.CompatCircuitCore.BinarySerialization;

namespace Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
public record BoolBeaverTripleShare : IBinaryEncodable, IGeneralBinaryDecodable<BoolBeaverTripleShare>, IEquatable<BoolBeaverTripleShare> {
    public required bool X { get; init; }
    public required bool Y { get; init; }
    public required bool XY { get; init; }

    public List<bool> ToList() => [this.X, this.Y, this.XY];
    public bool[] ToArray() => [this.X, this.Y, this.XY];
    public void Deconstruct(out bool x, out bool y, out bool xy) => (x, y, xy) = (this.X, this.Y, this.XY);

    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        byte result = 0;
        if (this.X) {
            result |= 1;    // Set the 0th bit if X is true
        }

        if (this.Y) {
            result |= 2;    // Set the 1st bit if Y is true
        }

        if (this.XY) {
            result |= 4;   // Set the 2nd bit if XY is true
        }

        destination[0] = result;
        bytesWritten = 1;
    }

    public int GetEncodedByteCount() => 1;

    public static BoolBeaverTripleShare FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead) {
        byte b = buffer[0];
        bool x = (b & 1) != 0;    // Check the 0th bit
        bool y = (b & 2) != 0;    // Check the 1st bit
        bool xy = (b & 4) != 0;   // Check the 2nd bit
        bytesRead = 1;
        return new BoolBeaverTripleShare { X = x, Y = y, XY = xy };
    }

}
