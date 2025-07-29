using System.Numerics;

namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Messages;
public class BigIntegerInputShareMessagePayload(string exposureKey, int shareOwnerID, IEnumerable<BigInteger> values) : BigIntegerExposureMessagePayload(exposureKey, shareOwnerID, values), IMessagePayload<BigIntegerInputShareMessagePayload> {
    public static new byte MessagePayloadTypeID => (byte)MessagePayloadType.BigIntegerInputShare;

    public static new BigIntegerInputShareMessagePayload FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead) {
        BigIntegerExposureMessagePayload payload = BigIntegerExposureMessagePayload.FromEncodedBytes(buffer, out bytesRead);
        return new BigIntegerInputShareMessagePayload(payload.ExposureKey, payload.ShareOwnerID, payload.Values);
    }
}
