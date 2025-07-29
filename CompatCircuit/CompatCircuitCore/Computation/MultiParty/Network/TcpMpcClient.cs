using Anonymous.CompatCircuitCore.Computation.MultiParty.Messages;
using Anonymous.CompatCircuitCore.Computation.MultiParty.Network.NetClients;
using Anonymous.CompatCircuitCore.Computation.MultiParty.Network.NetMessages;
using Anonymous.CompatCircuitCore.Extensions;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Network;
public class TcpMpcClient : IMpcClient, IDisposable {
    // TODO: experimental. Not tested.
    public INetClientFactory NetClientFactory { get; set; }
    private INetClient? DataNetClient { get; set; }

    private bool disposedValue;

    public event EventHandler<RawMessage>? OnMessageReceived;
    protected NetMpcClientConfig MyConfig { get; }
    protected int PartyCount => this.MyConfig.PartyDataEndpoints.Count;
    protected int MyID => this.MyConfig.MyID;

    private CancellationTokenSource? CancellationTokenSource { get; set; } = null;

    private long _totalBytesSent = 0;
    public long TotalBytesSent => this._totalBytesSent;

    public void Start() {
        if (this.DataNetClient is not null) {
            throw new InvalidOperationException("The client is already started.");
        }

        this.DataNetClient = this.NetClientFactory.NewNetClient();
        this.DataNetClient.OnPayloadReceived += this.HandleClientMessage;
        this.DataNetClient.Start(this.MyConfig.MyDataEndpoint);

        Serilog.Log.Debug("TcpMpcClient: started.");
    }

    // receiverID -> messageID -> (message, timeoutTimestampMS)
    private IReadOnlyList<ConcurrentDictionary<int, (NetRawMessage message, long timeoutTimestampMS)>> SendBuffer { get; }

    public TcpMpcClient(NetMpcClientConfig myMpcClientConfig) {
        this.MyConfig = myMpcClientConfig;
        this.SendBuffer = Enumerable.Range(0, this.PartyCount).Select(_ => new ConcurrentDictionary<int, (NetRawMessage, long)>()).ToList();
        this.NetClientFactory = new TcpNetClientFactory();
    }

    public TcpMpcClient(NetMpcClientConfig myMpcClientConfig, INetClientFactory netClientFactory) {
        this.MyConfig = myMpcClientConfig;
        this.SendBuffer = Enumerable.Range(0, this.PartyCount).Select(_ => new ConcurrentDictionary<int, (NetRawMessage, long)>()).ToList();
        this.NetClientFactory = netClientFactory;
    }

    private IReadOnlyList<int> AllPartiesButMe => Enumerable.Range(0, this.PartyCount).Where(x => x != this.MyID).ToList();

    private static string GetCrc32String(byte[] data) => BitConverter.ToString(System.IO.Hashing.Crc32.Hash(data)).Replace("-", string.Empty);
    public void BroadcastMessage(RawMessage message) {
        int rawMessageLength = message.GetEncodedByteCount();
        byte[] data = new byte[rawMessageLength];
        message.EncodeBytes(data, out _);

        foreach (int receiverIndex in this.AllPartiesButMe) {
            Serilog.Log.Verbose($"TcpMpcClient: Broadcast RawMessage. Sender: {this.MyID}, Receiver: {receiverIndex}, Size: {data.Length}, CRC32: {GetCrc32String(data)}");

            this.DataNetClient.SendPayload(this.MyConfig.PartyDataEndpoints[receiverIndex], data);
            _ = Interlocked.Add(ref this._totalBytesSent, rawMessageLength);
        }
    }

    public void SendMessage(int receiverIndex, RawMessage message) {
        int rawMessageLength = message.GetEncodedByteCount();
        byte[] data = new byte[rawMessageLength];
        message.EncodeBytes(data, out _);

        Serilog.Log.Verbose($"TcpMpcClient: Send RawMessage. Sender: {this.MyID}, Receiver: {receiverIndex}, Size: {data.Length}, CRC32: {GetCrc32String(data)}");

        this.DataNetClient.SendPayload(this.MyConfig.PartyDataEndpoints[receiverIndex], data);
        _ = Interlocked.Add(ref this._totalBytesSent, rawMessageLength);
    }

    private void HandleClientMessage(object sender, byte[] payload) {
        try {
            RawMessage msg = RawMessage.FromEncodedBytes(payload, out int bytesRead);
            if (bytesRead != payload.Length) {
                Serilog.Log.Warning("The number of bytes read does not match the expected value. This may be caused by a network issue.");
            }
            this.OnMessageReceived?.Invoke(this, msg);
        }
        catch (Exception ex) {
            Serilog.Log.Warning($"TcpMpcClient: Error in decoding a packet: {ex}", ex);

            Debugger.Break();
        }
    }

    protected virtual void Dispose(bool disposing) {
        if (!this.disposedValue) {
            if (disposing) {
                // dispose managed state (managed objects)
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            this.Stop();

            // set large fields to null

            this.disposedValue = true;
        }
    }

    // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~TcpMpcClient() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: false);
    }

    public void Dispose() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void Stop() {
        this.DataNetClient?.Stop();
        this.DataNetClient = null;
    }

}
