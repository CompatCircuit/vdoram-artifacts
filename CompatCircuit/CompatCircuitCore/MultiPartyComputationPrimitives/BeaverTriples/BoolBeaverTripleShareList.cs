using Anonymous.CompatCircuitCore.BinarySerialization;
using Anonymous.CompatCircuitCore.Extensions;
using System.Buffers.Binary;
using System.Collections;

namespace Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
public class BoolBeaverTripleShareList : List<BoolBeaverTripleShare>, IBinaryEncodable, IGeneralBinaryDecodable<BoolBeaverTripleShareList> {
    public int GetEncodedByteCount() => sizeof(int) + BitArrayHelper.BitCountToByteCount(3 * this.Count);
    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        byte[] bytes = new byte[sizeof(int) + BitArrayHelper.BitCountToByteCount(3 * this.Count)];
        // Heading 4 bytes: the number of triples (each triple contains 3 bits)
        BinaryPrimitives.WriteInt32LittleEndian(bytes, this.Count);

        BitArray bitArray = new(3 * this.Count);
        for (int i = 0; i < this.Count; i++) {
            bitArray[3 * i] = this[i].X;
            bitArray[(3 * i) + 1] = this[i].Y;
            bitArray[(3 * i) + 2] = this[i].XY;
        }
        bitArray.CopyTo(bytes, sizeof(int));

        bytes.CopyTo(destination);
        bytesWritten = bytes.Length;
    }
    public static BoolBeaverTripleShareList FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead) {
        int count = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        BoolBeaverTripleShareList list = [];

        int bitCount = 3 * count;
        BitArray bitArray = new(buffer.Slice(sizeof(int), BitArrayHelper.BitCountToByteCount(bitCount)).ToArray()) { Length = bitCount };
        for (int i = 0; i < count; i++) {
            BoolBeaverTripleShare share = new() {
                X = bitArray[3 * i],
                Y = bitArray[(3 * i) + 1],
                XY = bitArray[(3 * i) + 2],
            };
            list.Add(share);
        }

        bytesRead = sizeof(int) + BitArrayHelper.BitCountToByteCount(bitCount);
        return list;
    }
}
