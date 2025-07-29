using Anonymous.CompatCircuitCore.Computation.MultiParty.Messages;
using Anonymous.CompatCircuitCore.Computation.MultiParty.Network.NetClients;
using Anonymous.CompatCircuitCore.Computation.MultiParty.Network.NetMessages;
using Anonymous.CompatCircuitCore.Extensions;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Network;
public class UdpMpcClient : IMpcClient, IDisposable {
    public INetClientFactory NetClientFactory { get; set; }
    private INetClient? DataNetClient { get; set; }
    private INetClient? AckNetClient { get; set; }
    private bool UseDedicatedAckPort { get; set; } = false; // Require more CPU cores and I don't see performance improvements.

    private bool disposedValue;

    public event EventHandler<RawMessage>? OnMessageReceived;
    protected NetMpcClientConfig MyConfig { get; }
    protected int PartyCount => this.MyConfig.PartyDataEndpoints.Count;
    protected int MyID => this.MyConfig.MyID;

    private Task? Task { get; set; } = null;

    private CancellationTokenSource? CancellationTokenSource { get; set; } = null;

    private long _totalBytesSent = 0;
    public long TotalBytesSent => this._totalBytesSent;

    public void Start() {
        if (this.Task is not null) {
            throw new InvalidOperationException("The client is already started.");
        }

        this.CancellationTokenSource = new CancellationTokenSource();
        this.Task = Task.Run(() => this.RunAsync(this.CancellationTokenSource.Token));
    }

    // receiverID -> messageID -> (message, timeoutTimestampMS)
    private IReadOnlyList<ConcurrentDictionary<int, (NetRawMessage message, long timeoutTimestampMS)>> SendBuffer { get; }

    public UdpMpcClient(NetMpcClientConfig myMpcClientConfig) {
        this.MyConfig = myMpcClientConfig;
        this.SendBuffer = Enumerable.Range(0, this.PartyCount).Select(_ => new ConcurrentDictionary<int, (NetRawMessage, long)>()).ToList();
        this.NetClientFactory = new UdpNetClientFactory();
    }

    public UdpMpcClient(NetMpcClientConfig myMpcClientConfig, INetClientFactory netClientFactory) {
        this.MyConfig = myMpcClientConfig;
        this.SendBuffer = Enumerable.Range(0, this.PartyCount).Select(_ => new ConcurrentDictionary<int, (NetRawMessage, long)>()).ToList();
        this.NetClientFactory = netClientFactory;
    }

    private IReadOnlyList<int> AllPartiesButMe => Enumerable.Range(0, this.PartyCount).Where(x => x != this.MyID).ToList();

    private int _nextNetRawMessageID = 0;

    private int IncreaseNextNetRawMessageID() => Interlocked.Increment(ref this._nextNetRawMessageID);

    private static string GetCrc32String(byte[] data) => BitConverter.ToString(System.IO.Hashing.Crc32.Hash(data)).Replace("-", string.Empty);
    public void BroadcastMessage(RawMessage message) {
        int rawMessageLength = message.GetEncodedByteCount();
        byte[] data = new byte[rawMessageLength];
        message.EncodeBytes(data, out _);

        int messageID = this.IncreaseNextNetRawMessageID();

        foreach (int receiverIndex in this.AllPartiesButMe) {
            NetRawMessage netRawMessage = NetRawMessageHelper.ComposeNetRawMessage(messageID, this.MyID, receiverIndex, new NetDataMessagePayload(data));
            Serilog.Log.Verbose($"UdpMpcClient: Broadcast NetRawMessage. Message ID: {netRawMessage.MessageID}, Sender: {netRawMessage.SenderID}, Receiver: {netRawMessage.ReceiverID}, Size: {data.Length}, CRC32: {GetCrc32String(data)}");
            this.SendNetRawMessage(netRawMessage);
            _ = Interlocked.Add(ref this._totalBytesSent, rawMessageLength);
        }
    }

    public void SendMessage(int receiverIndex, RawMessage message) {
        int rawMessageLength = message.GetEncodedByteCount();
        byte[] data = new byte[rawMessageLength];
        message.EncodeBytes(data, out _);

        int messageID = this.IncreaseNextNetRawMessageID();
        NetRawMessage netRawMessage = NetRawMessageHelper.ComposeNetRawMessage(messageID, this.MyID, receiverIndex, new NetDataMessagePayload(data));
        Serilog.Log.Verbose($"UdpMpcClient: Send NetRawMessage. Message ID: {netRawMessage.MessageID}, Sender: {netRawMessage.SenderID}, Receiver: {netRawMessage.ReceiverID}, Size: {data.Length}, CRC32: {GetCrc32String(data)}");
        this.SendNetRawMessage(netRawMessage);
        _ = Interlocked.Add(ref this._totalBytesSent, rawMessageLength);
    }

    private void SendNetRawMessage(NetRawMessage netRawMessage) {
        Trace.Assert(this.DataNetClient is not null);
        Trace.Assert(this.UseDedicatedAckPort == this.AckNetClient is not null);

        Serilog.Log.Verbose($"UdpMpcClient: Send NetRawMessage. Message ID: {netRawMessage.MessageID}, Sender: {netRawMessage.SenderID}, Receiver: {netRawMessage.ReceiverID}");

        // Store the message until it is acknowledged
        if (netRawMessage.MessagePayloadTypeEnum != NetMessagePayloadType.Ack) {
            long currentTimestampMS = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            int ackTimeoutMS = this.MyConfig.TimeoutMS;
            this.SendBuffer[netRawMessage.ReceiverID][netRawMessage.MessageID] = (message: netRawMessage, timeoutTimestampMS: currentTimestampMS + ackTimeoutMS);
        }

        // Prepare the payload
        int payloadLength = netRawMessage.GetEncodedByteCount();
        byte[] payload = new byte[payloadLength];
        netRawMessage.EncodeBytes(payload, out _);

        if (this.UseDedicatedAckPort && netRawMessage.MessagePayloadTypeEnum == NetMessagePayloadType.Ack) {
            this.AckNetClient!.SendPayload(this.MyConfig.PartyAckEndpoints[netRawMessage.ReceiverID], payload);
        }
        else {
            this.DataNetClient.SendPayload(this.MyConfig.PartyDataEndpoints[netRawMessage.ReceiverID], payload);
        }
    }

    private async Task HandleAckAsync(CancellationToken cancellationToken) => await AsyncHelper.TerminateOnException(async () => {
        Trace.Assert(this.DataNetClient is not null);
        Trace.Assert(this.UseDedicatedAckPort == this.AckNetClient is not null);

        int loopCount = 0;
        int loopModulus = 100;
        while (true) {
            loopCount = (loopCount + 1) % loopModulus;

            int unAckPackets = this.SendBuffer.Select(buffer => buffer.Count).Sum();

            if (loopCount == 0) {
                Serilog.Log.Verbose($"UdpMpcClient: HandleAckAsync loop iteration. Remaining packets: {unAckPackets}");
            }

            // cancellationToken.ThrowIfCancellationRequested();
            if (cancellationToken.IsCancellationRequested) {
                if (unAckPackets != 0) {
                    Serilog.Log.Warning($"UdpMpcClient: there are still {unAckPackets} packets waiting to be confirmed. Let me re-send these packets for the last time.");
                    foreach (int receiverID in this.AllPartiesButMe) {
                        foreach ((int messageID, (NetRawMessage netRawMessage, long timeoutTimestampMS)) in this.SendBuffer[receiverID]) {
                            this.SendNetRawMessage(netRawMessage);
                        }
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();
            }

            long currentTimestampMS = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            foreach (int receiverID in this.AllPartiesButMe) {
                foreach ((int messageID, (NetRawMessage netRawMessage, long timeoutTimestampMS)) in this.SendBuffer[receiverID]) {
                    if (timeoutTimestampMS < currentTimestampMS) {
                        // Resend the message
                        this.SendNetRawMessage(netRawMessage);
                        Serilog.Log.Warning($"UdpMpcClient: Timeout. Resend NetRawMessage. Message ID: {netRawMessage.MessageID} (receiverID: {receiverID})");
                    }
                }
            }

            await Task.Delay(100); // TODO: make this configurable
        }
    });

    private void HandleClientMessage(object sender, byte[] payload) {
        try {
            NetRawMessage msg = NetRawMessage.FromEncodedBytes(payload, out int bytesRead);
            if (bytesRead != payload.Length) {
                Serilog.Log.Warning("The number of bytes read does not match the expected value. This may be caused by a network issue.");
            }
            this.HandleNetRawMessage(msg);
        }
        catch (Exception ex) {
            Serilog.Log.Warning($"UdpMpcClient: Error in decoding a packet: {ex}", ex);

            Debugger.Break();
        }
    }

    public void HandleNetRawMessage(NetRawMessage msg) {
        if (msg.ReceiverID != this.MyID) {
            Serilog.Log.Warning($"UdpMpcClient: IgnoredNetRawMessage with wrong receiver: {msg.ReceiverID}");
            return;
        }

        Serilog.Log.Verbose($"UdpMpcClient: Received NetRawMessage. Message ID: {msg.MessageID}, Sender: {msg.SenderID}");

        switch (msg.MessagePayloadTypeEnum) {
            case NetMessagePayloadType.Data:
                NetDataMessagePayload dataMessagePayload = msg.ExtractMessagePayload<NetDataMessagePayload>();
                RawMessage rawMessage = RawMessage.FromEncodedBytes(dataMessagePayload.Data, out _);
                this.OnMessageReceived?.Invoke(this, rawMessage);
                Serilog.Log.Verbose($"UdpMpcClient: Received a data message. Message ID: {msg.MessageID}, Sender: {msg.SenderID}, Size: {dataMessagePayload.Data.Length}, CRC32: {GetCrc32String(dataMessagePayload.Data)}");
                break;
            case NetMessagePayloadType.Ack:
                NetAckMessagePayload ackMessagePayload = msg.ExtractMessagePayload<NetAckMessagePayload>();
                _ = this.SendBuffer[msg.SenderID].TryRemove(ackMessagePayload.MessageID, out _);
                Serilog.Log.Verbose($"UdpMpcClient: Received an acknowledgment. Message ID: {msg.MessageID}, Sender: {msg.SenderID}, AckMessageID: {ackMessagePayload.MessageID}");
                break;
            default:
                throw new InvalidDataException("Unrecognized payload type.");
        }

        if (msg.MessagePayloadTypeEnum != NetMessagePayloadType.Ack) {
            // Send an acknowledgment
            this.SendNetRawMessage(NetRawMessageHelper.ComposeNetRawMessage(messageID: -1, senderID: this.MyID, receiverID: msg.SenderID, new NetAckMessagePayload() { MessageID = msg.MessageID }));
        }

    }

    private async Task RunAsync(CancellationToken cancellationToken) => await AsyncHelper.TerminateOnException(async () => {
        if (this.DataNetClient is not null) {
            throw new InvalidOperationException("The client is already started.");
        }

        this.DataNetClient = this.NetClientFactory.NewNetClient();
        this.DataNetClient.OnPayloadReceived += this.HandleClientMessage;
        this.DataNetClient.Start(this.MyConfig.MyDataEndpoint);

        if (this.UseDedicatedAckPort) {
            this.AckNetClient = this.NetClientFactory.NewNetClient();
            this.AckNetClient.OnPayloadReceived += this.HandleClientMessage;
            this.AckNetClient.Start(this.MyConfig.MyAckEndpoint);
        }

        Serilog.Log.Debug("UdpMpcClient: RunAsync started.");

        try {
            await this.HandleAckAsync(cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) {
            Serilog.Log.Error(ex.Message, ex);
            Debugger.Break();
            throw;
        }
        finally {
            this.DataNetClient?.Stop();
            this.DataNetClient = null;
            this.AckNetClient?.Stop();
            this.AckNetClient = null;
        }
    });

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
    ~UdpMpcClient() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: false);
    }

    public void Dispose() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void Stop() {
        if (this.Task is not null) {
            Task task = this.Task;
            this.CancellationTokenSource?.Cancel();
            this.CancellationTokenSource = null;
            this.Task = null;

            Serilog.Log.Information("Stopping UdpMpcClient...");
            _ = Task.WaitAny(task, Task.Delay(10000));
        }

        this.DataNetClient?.Stop();
        this.DataNetClient = null;
        this.AckNetClient?.Stop();
        this.AckNetClient = null;
    }

}
