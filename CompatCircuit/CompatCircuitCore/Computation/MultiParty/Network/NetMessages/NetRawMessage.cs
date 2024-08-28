using SadPencil.CompatCircuitCore.BinarySerialization;
using System.Buffers.Binary;
using System.Diagnostics;

namespace SadPencil.CompatCircuitCore.Computation.MultiParty.Network.NetMessages;
public class NetRawMessage : IBinaryEncodable, IGeneralBinaryDecodable<NetRawMessage> {
    public required int MessageID { get; init; }
    public required int SenderID { get; init; }
    public required int ReceiverID { get; init; }
    public required byte MessagePayloadType { get; init; }
    public required byte[] MessagePayloadBytes { get; init; }

    public NetMessagePayloadType MessagePayloadTypeEnum => (NetMessagePayloadType)this.MessagePayloadType;

    public static NetRawMessage FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead) {
        if (buffer.Length < sizeof(int) + sizeof(int) + sizeof(int) + sizeof(byte) + sizeof(int)) {
            throw new Exception($"Data incomplete. Length {buffer.Length} is too small");
        }
        bytesRead = 0;

        int messageID = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        int senderID = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        int receiverID = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        byte messageType = buffer[bytesRead..][0];
        bytesRead += sizeof(byte);

        int payloadBytesLength = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        if (buffer.Length - bytesRead != payloadBytesLength) {
            throw new Exception($"Data incomplete. Payload length {buffer.Length - bytesRead} (actual) != {payloadBytesLength} (claimed)");
        }

        byte[] payloadBytes = new byte[payloadBytesLength];
        buffer.Slice(bytesRead, payloadBytesLength).CopyTo(payloadBytes);
        bytesRead += payloadBytesLength;

        return new NetRawMessage() {
            MessageID = messageID,
            SenderID = senderID,
            ReceiverID = receiverID,
            MessagePayloadType = messageType,
            MessagePayloadBytes = payloadBytes,
        };
    }
    public int GetEncodedByteCount() => sizeof(int) + sizeof(int) + sizeof(int) + sizeof(byte) + sizeof(int) + this.MessagePayloadBytes.Length;
    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        bytesWritten = 0;

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.MessageID);
        bytesWritten += sizeof(int);

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.SenderID);
        bytesWritten += sizeof(int);

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.ReceiverID);
        bytesWritten += sizeof(int);

        destination[bytesWritten..][0] = this.MessagePayloadType;
        bytesWritten += sizeof(byte);

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.MessagePayloadBytes.Length);
        bytesWritten += sizeof(int);

        this.MessagePayloadBytes.CopyTo(destination[bytesWritten..]);
        bytesWritten += this.MessagePayloadBytes.Length;

        Trace.Assert(this.GetEncodedByteCount() == bytesWritten);
    }
}
