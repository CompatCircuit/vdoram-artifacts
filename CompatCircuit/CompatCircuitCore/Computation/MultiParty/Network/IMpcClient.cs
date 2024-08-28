using SadPencil.CompatCircuitCore.Computation.MultiParty.Messages;

namespace SadPencil.CompatCircuitCore.Computation.MultiParty.Network;
public interface IMpcClient {
    public void Start();
    public void Stop();
    public void BroadcastMessage(RawMessage message);
    public void SendMessage(int receiverIndex, RawMessage message);
    public event EventHandler<RawMessage>? OnMessageReceived;
}
