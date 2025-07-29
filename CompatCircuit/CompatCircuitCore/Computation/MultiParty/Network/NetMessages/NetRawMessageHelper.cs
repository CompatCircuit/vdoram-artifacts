namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Network.NetMessages;
public static class NetRawMessageHelper {
    public static NetRawMessage ComposeNetRawMessage<T>(int messageID, int senderID, int receiverID, T messagePayload) where T : INetMessagePayload<T> {
        byte[] payloadBytes = new byte[messagePayload.GetEncodedByteCount()];
        messagePayload.EncodeBytes(payloadBytes, out int _);
        return new NetRawMessage() { MessagePayloadBytes = payloadBytes, MessagePayloadType = T.MessagePayloadTypeID, MessageID = messageID, SenderID = senderID, ReceiverID = receiverID };
    }

    public static T ExtractMessagePayload<T>(this NetRawMessage msg) where T : INetMessagePayload<T> => Convert.ToByte(msg.MessagePayloadType) != T.MessagePayloadTypeID
        ? throw new ArgumentException($"Invalid message payload type. Expected {T.MessagePayloadTypeID}, got {msg.MessagePayloadType}.", nameof(T))
        : T.FromEncodedBytes(msg.MessagePayloadBytes, out int _);
}
