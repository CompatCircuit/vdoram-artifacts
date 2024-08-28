using SadPencil.CompatCircuitCore.Extensions;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;

namespace SadPencil.CompatCircuitCore.Computation.MultiParty.Messages;
public class BigIntegerExposureMessagePayload(string exposureKey, int shareOwnerID, IEnumerable<BigInteger> values) : IMessagePayload<BigIntegerExposureMessagePayload> {
    public static byte MessagePayloadTypeID => (byte)MessagePayloadType.BigIntegerExposure;

    public string ExposureKey { get; } = exposureKey;
    public List<BigInteger> Values { get; } = values.ToList();
    public int ShareOwnerID { get; } = shareOwnerID;

    public static BigIntegerExposureMessagePayload FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead) {
        bytesRead = 0;

        string exposureKey = StringHelper.FromBytes(buffer[bytesRead..], out int stringbytesRead);
        bytesRead += stringbytesRead;

        int shareOwnerID = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        int elementsCount = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        List<BigInteger> elements = [];
        for (int i = 0; i < elementsCount; i++) {
            int elementByteLength = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
            bytesRead += sizeof(int);

            byte[] elementByte = new byte[elementByteLength];
            buffer.Slice(bytesRead, elementByteLength).CopyTo(elementByte);
            bytesRead += elementByteLength;

            elements.Add(BigIntegerHelper.FromEncodedBytes(elementByte, out int _));
        }

        return new BigIntegerExposureMessagePayload(exposureKey, shareOwnerID, elements);
    }
    public int GetEncodedByteCount() => this.ExposureKey.GetWriteByteCount() + sizeof(int) + sizeof(int) + this.Values.Select(element => sizeof(int) + element.GetEncodedByteCount()).Sum();
    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        bytesWritten = 0;

        this.ExposureKey.WriteBytes(destination[bytesWritten..], out int strBytesWritten);
        bytesWritten += strBytesWritten;

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.ShareOwnerID);
        bytesWritten += sizeof(int);

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.Values.Count);
        bytesWritten += sizeof(int);

        foreach (BigInteger value in this.Values) {
            BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], value.GetEncodedByteCount());
            bytesWritten += sizeof(int);

            value.EncodeBytes(destination[bytesWritten..], out int valueBytesWritten);
            bytesWritten += valueBytesWritten;
        }

        Trace.Assert(this.GetEncodedByteCount() == bytesWritten);
    }
}
