using Anonymous.CompatCircuitCore.Extensions;
using System.Buffers.Binary;
using System.Collections;

namespace Anonymous.CompatCircuitCore.Extensions;
public static class BitArrayHelper {
    public static int BitCountToByteCount(int bitCount) => (bitCount + 7) / 8;
    public static int ByteCount(this BitArray bits) => BitCountToByteCount(bits.Count);

    public static IEnumerable<bool> AsEnumerable(this BitArray bitArray) {
        foreach (bool bit in bitArray) {
            yield return bit;
        }
    }

    public static List<bool> ToList(this BitArray bitArray) => bitArray.AsEnumerable().ToList();

    public static BitArray FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead) {
        bytesRead = 0;

        int bitsCount = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        int bytesCount = BitCountToByteCount(bitsCount);
        byte[] bitsBuffer = new byte[bytesCount];
        buffer.Slice(bytesRead, bytesCount).CopyTo(bitsBuffer);
        bytesRead += bytesCount;

        return new BitArray(bitsBuffer) { Length = bitsCount };
    }
    public static int GetEncodedByteCount(IReadOnlyList<bool> bits) => sizeof(int) + BitCountToByteCount(bits.Count);
    public static int GetEncodedByteCount(this BitArray bits) => sizeof(int) + bits.ByteCount();
    public static void EncodeBytes(this BitArray bits, Span<byte> destination, out int bytesWritten) {
        bytesWritten = 0;

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], bits.Count);
        bytesWritten += sizeof(int);

        byte[] buffer = new byte[bits.ByteCount()];
        bits.CopyTo(buffer, 0);
        buffer.CopyTo(destination[bytesWritten..]);
        bytesWritten += buffer.Length;
    }

}
