using System.Net;

namespace SadPencil.CompatCircuitCore.Computation.MultiParty.Network.NetClients;
public interface INetClient {
    public void Start(IPEndPoint endpoint);
    public void Stop();
    public void SendPayload(IPEndPoint endpoint, byte[] payload);
    public event EventHandler<byte[]>? OnPayloadReceived;
}
