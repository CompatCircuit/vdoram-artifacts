using Anonymous.CompatCircuitCore.Extensions;
using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Messages;
public class BoolExposureMessagePayload(string exposureKey, int shareOwnerID, BitArray bits) : IMessagePayload<BoolExposureMessagePayload> {
    public static byte MessagePayloadTypeID => (byte)MessagePayloadType.BoolExposure;

    public string ExposureKey { get; } = exposureKey;
    public BitArray Bits { get; } = new BitArray(bits);
    public int ShareOwnerID { get; } = shareOwnerID;
    public static BoolExposureMessagePayload FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead) {
        bytesRead = 0;

        string exposureKey = StringHelper.FromBytes(buffer[bytesRead..], out int stringbytesRead);
        bytesRead += stringbytesRead;

        int shareOwnerID = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        BitArray bits = BitArrayHelper.FromEncodedBytes(buffer[bytesRead..], out int bitsBytesRead);
        bytesRead += bitsBytesRead;

        return new BoolExposureMessagePayload(exposureKey, shareOwnerID, bits);
    }
    public int GetEncodedByteCount() => this.ExposureKey.GetWriteByteCount() + sizeof(int) + this.Bits.GetEncodedByteCount();
    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        bytesWritten = 0;

        this.ExposureKey.WriteBytes(destination[bytesWritten..], out int strBytesWritten);
        bytesWritten += strBytesWritten;

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.ShareOwnerID);
        bytesWritten += sizeof(int);

        this.Bits.EncodeBytes(destination[bytesWritten..], out int bitsBytesWritten);
        bytesWritten += bitsBytesWritten;

        Trace.Assert(this.GetEncodedByteCount() == bytesWritten);
    }
}
