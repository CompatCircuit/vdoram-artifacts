using Anonymous.CompatCircuitCore.Computation.MultiParty.Messages;

namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Network;
public class DummyMpcClient : IMpcClient {
    public event EventHandler<RawMessage>? OnMessageReceived { add { } remove { } } // https://stackoverflow.com/a/3675077

    public void BroadcastMessage(RawMessage message) { }
    public void SendMessage(int receiverIndex, RawMessage message) { }
    public void Start() { }
    public void Stop() { }
}
