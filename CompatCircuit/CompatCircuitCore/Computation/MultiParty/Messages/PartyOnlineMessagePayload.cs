using System.Buffers.Binary;

namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Messages;
public class PartyOnlineMessagePayload(int partyID) : IMessagePayload<PartyOnlineMessagePayload> {
    public static byte MessagePayloadTypeID => (byte)MessagePayloadType.PartyOnline;
    public int PartyID { get; } = partyID;

    public static PartyOnlineMessagePayload FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead) {
        int clientID = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        bytesRead = sizeof(int);
        return new PartyOnlineMessagePayload(clientID);
    }
    public int GetEncodedByteCount() => sizeof(int);
    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        BinaryPrimitives.WriteInt32LittleEndian(destination, this.PartyID);
        bytesWritten = sizeof(int);
    }
}
