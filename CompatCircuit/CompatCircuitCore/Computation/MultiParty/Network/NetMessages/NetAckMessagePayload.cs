using System.Buffers.Binary;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Network.NetMessages;
public class NetAckMessagePayload : INetMessagePayload<NetAckMessagePayload> {
    public static byte MessagePayloadTypeID => (byte)NetMessagePayloadType.Ack;
    public required int MessageID { get; init; }

    public static NetAckMessagePayload FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead) {
        bytesRead = 0;

        int messageID = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);
        return new NetAckMessagePayload {
            MessageID = messageID,
        };
    }

    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        bytesWritten = 0;

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.MessageID);
        bytesWritten += sizeof(int);

        Trace.Assert(bytesWritten == this.GetEncodedByteCount());
    }
    public int GetEncodedByteCount() => sizeof(int);
}
