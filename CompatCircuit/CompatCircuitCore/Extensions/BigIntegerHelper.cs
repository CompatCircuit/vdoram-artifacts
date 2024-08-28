using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics;
using System.Numerics;

namespace SadPencil.CompatCircuitCore.Extensions;
public static class BigIntegerHelper {
    public static void EncodeBytes(this BigInteger value, Span<byte> destination, out int bytesWritten, bool isUnsigned = true, bool isBigEndian = false) {
        int bigIntByteCount = value.GetByteCount(isUnsigned);
        BinaryPrimitives.WriteInt32LittleEndian(destination, bigIntByteCount);
        bytesWritten = sizeof(int);

        bool fitted = value.TryWriteBytes(destination[bytesWritten..], out int bigIntBytesWritten, isUnsigned, isBigEndian);
        if (!fitted) {
            throw new ArgumentOutOfRangeException(nameof(destination), "The destination span is too small to contain BigInteger");
        }
        Trace.Assert(bigIntBytesWritten == bigIntByteCount);

        bytesWritten += bigIntBytesWritten;
    }

    public static int GetEncodedByteCount(this BigInteger value, bool isUnsigned = true) => sizeof(int) + value.GetByteCount(isUnsigned);

    public static BigInteger FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead, bool isUnsigned = true, bool isBigEndian = false) {
        int byteCount = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        BigInteger value = new(buffer.Slice(sizeof(int), byteCount), isUnsigned, isBigEndian);
        bytesRead = sizeof(int) + byteCount;
        return value;
    }

    public static bool[] BitDecompositionUnsigned(this BigInteger value) => value.BitDecompositionUnsigned(Convert.ToInt32(value.GetBitLength()));
    public static bool[] BitDecompositionUnsigned(this BigInteger value, int desiredbitsLength) {
        if (value.Sign == -1) {
            throw new ArgumentException("BigIntegerHelper.BitDecomposition() is designed for unsigned values.", nameof(value));
        }
        BitArray bits = new(value.ToByteArray(isUnsigned: true, isBigEndian: false)) { Length = desiredbitsLength };
        //List<bool> resultBits = [];
        //if (bits.Length >= desiredbitsLength) {
        //    resultBits.AddRange(bits.AsEnumerable().Take(desiredbitsLength));
        //}
        //else {
        //    resultBits.AddRange(bits.AsEnumerable());
        //    resultBits.AddRange(Enumerable.Repeat(false, desiredbitsLength - bits.Length));
        //}
        //return resultBits.ToArray();
        return bits.AsEnumerable().ToArray();
    }

    public static BigInteger FromBitDecompositionUnsigned(bool[] bits) {
        byte[] buffer = new byte[BitArrayHelper.BitCountToByteCount(bits.Length)];
        new BitArray(bits).CopyTo(buffer, 0);
        return new(buffer, isUnsigned: true, isBigEndian: false);
    }
}
