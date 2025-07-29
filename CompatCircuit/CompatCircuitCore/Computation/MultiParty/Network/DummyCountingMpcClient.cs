using Anonymous.CompatCircuitCore.Computation.MultiParty.Messages;

namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Network;
public class DummyCountingMpcClient : IMpcClient {
    private long _totalBytesSent = 0;
    public long TotalBytesSent => this._totalBytesSent;

    public int PartyCount { get; }

    public DummyCountingMpcClient(int partyCount) {
        if (partyCount <= 0) {
            throw new ArgumentOutOfRangeException(nameof(partyCount), "Party count should be a positive integer");
        }

        this.PartyCount = partyCount;
    }

    public event EventHandler<RawMessage>? OnMessageReceived { add { } remove { } } // https://stackoverflow.com/a/3675077

    public void BroadcastMessage(RawMessage message) => _ = Interlocked.Add(ref this._totalBytesSent, message.GetEncodedByteCount() * (this.PartyCount - 1));

    public void SendMessage(int receiverIndex, RawMessage message) => _ = Interlocked.Add(ref this._totalBytesSent, message.GetEncodedByteCount());

    public void Start() { }

    public void Stop() { }
}
