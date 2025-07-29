using Anonymous.CompatCircuitCore.Extensions;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Network.NetClients;
public class UdpNetClient : INetClient {
    private UdpClient? UdpClient { get; set; } = null;

    public event EventHandler<byte[]>? OnPayloadReceived;

    private Task? ServerTask { get; set; } = null;
    private CancellationTokenSource? CancellationTokenSource { get; set; } = null;

    public void SendPayload(IPEndPoint endpoint, byte[] payload) {
        if (this.UdpClient is null) {
            throw new InvalidOperationException("Please start UdpNetClient first.");
        }

        WrapPayload(payload.ToArray(), out byte[] udpBuffer);
        // TODO: is it thread-safe?
        _ = this.UdpClient.Send(udpBuffer, endpoint);
    }
    public void Start(IPEndPoint endpoint) {
        if (this.ServerTask is not null) {
            throw new InvalidOperationException("UdpNetCilent is already started");
        }

        this.UdpClient = new UdpClient(endpoint);

        this.CancellationTokenSource = new CancellationTokenSource();
        this.ServerTask = Task.Run(() => this.HandleIncomingAsync(this.CancellationTokenSource.Token));
    }

    public void Stop() {
        if (this.ServerTask is not null) {
            Task serverTask = this.ServerTask;
            this.CancellationTokenSource?.Cancel();
            this.CancellationTokenSource = null;
            this.ServerTask = null;

            Serilog.Log.Information("Stopping UdpNetClient...");
            _ = Task.WaitAny(serverTask, Task.Delay(10000));
        }

        this.UdpClient?.Close();
        this.UdpClient = null;
    }

    private async Task HandleIncomingAsync(CancellationToken cancellationToken) => await AsyncHelper.TerminateOnException(async () => {
        Trace.Assert(this.UdpClient is not null, "UdpClient should not be null");

        while (true) {
            cancellationToken.ThrowIfCancellationRequested();

            UdpReceiveResult res;
            try {
                res = await this.UdpClient.ReceiveAsync(cancellationToken);
                Serilog.Log.Verbose($"UdpNetClient: Received UDP message. Length: {res.Buffer.Length}; Sender: {res.RemoteEndPoint}");

                if (UnwrapPayload(res.Buffer, out int payloadLength, out byte[]? payload)) {
                    Trace.Assert(payload is not null);
                    this.OnPayloadReceived?.Invoke(this, payload);
                }
            }
            catch (Exception ex) when (ex is ObjectDisposedException or SocketException) {
                Serilog.Log.Warning(ex.Message);
                continue;
            }
            catch (Exception) {
                throw;
            }
        }
    });

    private static readonly IReadOnlyList<byte> PayloadWrapStartIndicator = [0x11, 0x45, 0x14, 0x19, 0x19, 0x81, 0x00];

    private static void WrapPayload(byte[] payload, out byte[] wrapBuffer) {
        byte[] size = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(size, payload.Length);
        wrapBuffer = PayloadWrapStartIndicator.Concat(size).Concat(payload).ToArray();
    }

    private static bool UnwrapPayload(byte[] wrapBuffer, out int payloadLength, out byte[]? payload) {
        int startIndex = IReadOnlyListHelper.FindPattern(wrapBuffer, PayloadWrapStartIndicator);
        if (startIndex != 0) {
            Serilog.Log.Warning("UdpNetClient: Unrecognized UDP packet received");
            // Drop unrecognized UDP packets
            payloadLength = -1;
            payload = null;
            return false;
        }

        payloadLength = BinaryPrimitives.ReadInt32LittleEndian(wrapBuffer.AsSpan()[(startIndex + PayloadWrapStartIndicator.Count)..]);
        if (payloadLength > wrapBuffer.Length - startIndex - PayloadWrapStartIndicator.Count - sizeof(int)) {
            Serilog.Log.Warning("UdpNetClient: Incomplete UDP packet received");
            // Drop oversized packets (usually caused by a network issue)
            payloadLength = -1;
            payload = null;
            return false;
        }

        payload = wrapBuffer.AsSpan().Slice(startIndex + PayloadWrapStartIndicator.Count + sizeof(int), payloadLength).ToArray();

        return true;
    }

}
