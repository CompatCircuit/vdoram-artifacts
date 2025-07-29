using Anonymous.CompatCircuitCore.BinarySerialization;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Messages;
public class RawMessage : IBinaryEncodable, IGeneralBinaryDecodable<RawMessage> {
    public required int SessionID { get; init; }
    public required byte MessagePayloadType { get; init; }
    public required byte[] MessagePayloadBytes { get; init; }

    public MessagePayloadType MessagePayloadTypeEnum => (MessagePayloadType)this.MessagePayloadType;

    public static RawMessage FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead) {
        bytesRead = 0;

        int sessionID = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        byte messageType = buffer[bytesRead..][0];
        bytesRead += sizeof(byte);

        int payloadBytesLength = BinaryPrimitives.ReadInt32LittleEndian(buffer[bytesRead..]);
        bytesRead += sizeof(int);

        byte[] payloadBytes = new byte[payloadBytesLength];
        buffer.Slice(bytesRead, payloadBytesLength).CopyTo(payloadBytes);
        bytesRead += payloadBytesLength;

        return new RawMessage() {
            SessionID = sessionID,
            MessagePayloadType = messageType,
            MessagePayloadBytes = payloadBytes,
        };
    }
    public int GetEncodedByteCount() => sizeof(int) + sizeof(byte) + sizeof(int) + this.MessagePayloadBytes.Length;
    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        bytesWritten = 0;

        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], this.SessionID);
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
