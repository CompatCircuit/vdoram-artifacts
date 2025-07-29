namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Messages;
public class PartyCompletedMessagePayload(int partyID) : PartyOnlineMessagePayload(partyID), IMessagePayload<PartyCompletedMessagePayload> {
    public static new byte MessagePayloadTypeID => (byte)MessagePayloadType.PartyCompleted;
    public static new PartyCompletedMessagePayload FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead) {
        PartyOnlineMessagePayload payload = PartyOnlineMessagePayload.FromEncodedBytes(buffer, out bytesRead);
        return new PartyCompletedMessagePayload(payload.PartyID);
    }
}
