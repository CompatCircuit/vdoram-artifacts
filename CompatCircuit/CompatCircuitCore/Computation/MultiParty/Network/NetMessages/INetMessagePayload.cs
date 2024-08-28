using SadPencil.CompatCircuitCore.BinarySerialization;

namespace SadPencil.CompatCircuitCore.Computation.MultiParty.Network.NetMessages;
public interface INetMessagePayload<T> : IBinaryEncodable, IGeneralBinaryDecodable<T> where T : INetMessagePayload<T> {
    public static abstract byte MessagePayloadTypeID { get; }
}