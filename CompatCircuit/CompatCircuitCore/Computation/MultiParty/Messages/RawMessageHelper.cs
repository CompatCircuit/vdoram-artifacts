namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Messages;
public static class RawMessageHelper {
    public static RawMessage ComposeRawMessage<T>(int sessionID, T messagePayload) where T : IMessagePayload<T> {
        byte[] payloadBytes = new byte[messagePayload.GetEncodedByteCount()];
        messagePayload.EncodeBytes(payloadBytes, out int _);
        return new RawMessage() { SessionID = sessionID, MessagePayloadBytes = payloadBytes, MessagePayloadType = T.MessagePayloadTypeID };
    }

    public static T ExtractMessagePayload<T>(this RawMessage msg) where T : IMessagePayload<T> => Convert.ToByte(msg.MessagePayloadType) != T.MessagePayloadTypeID
        ? throw new ArgumentException($"Invalid message payload type. Expected {T.MessagePayloadTypeID}, got {msg.MessagePayloadType}.", nameof(T))
        : T.FromEncodedBytes(msg.MessagePayloadBytes, out int _);
}
