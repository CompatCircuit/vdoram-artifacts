using System.Buffers.Binary;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Network.NetMessages;
public class NetDataMessagePayload(byte[] data) : INetMessagePayload<NetDataMessagePayload> {
    public static byte MessagePayloadTypeID => (byte)NetMessagePayloadType.Data;
    public byte[] Data { get; } = data;
    public static NetDataMessagePayload FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead) {
        bytesRead = 0;

        int dataLength = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        byte[] data = buffer[bytesRead..][..dataLength].ToArray();
        bytesRead += dataLength;

        return new NetDataMessagePayload(data);
    }

    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        bytesWritten = 0;

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.Data.Length);
        bytesWritten += sizeof(int);

        this.Data.CopyTo(destination[bytesWritten..]);
        bytesWritten += this.Data.Length;

        Trace.Assert(bytesWritten == this.GetEncodedByteCount());
    }

    public int GetEncodedByteCount() => sizeof(int) + this.Data.Length;
}
