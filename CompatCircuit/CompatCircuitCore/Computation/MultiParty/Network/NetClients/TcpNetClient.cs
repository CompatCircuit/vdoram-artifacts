using SadPencil.CompatCircuitCore.Extensions;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace SadPencil.CompatCircuitCore.Computation.MultiParty.Network.NetClients;
public class TcpNetClient : INetClient {
    private Dictionary<EndPoint, Socket> ClientSockets { get; set; } = [];

    public TcpNetClient() { }

    // TODO: needs to carefully re-implement the ACK mechanism for TcpNetClient. Currently TcpNetClient is almost unusable. Please use UdpNetClient for now!

    public event EventHandler<byte[]>? OnPayloadReceived;

    private async Task<Socket> DialAndUpdateClientSocket(EndPoint endpoint) {
        Socket clientSocket = new(SocketType.Stream, ProtocolType.Tcp);
        await clientSocket.ConnectAsync(endpoint);
        return this.ClientSockets[endpoint] = clientSocket; // TODO: possible concurrent issue here?
    }

    private async Task<Socket> GetOrDialClientSocket(EndPoint endpoint) {
        if (this.ClientSockets.TryGetValue(endpoint, out Socket? clientSocket)) {
            Trace.Assert(clientSocket is not null);
            if (!clientSocket.Connected) {
                clientSocket.Close();
                return await this.DialAndUpdateClientSocket(endpoint);
            }
            else {
                return clientSocket;
            }
        }
        else {
            return await this.DialAndUpdateClientSocket(endpoint);
        }
    }

    private async Task HandleIncomingAsync(CancellationToken cancellationToken, Socket socket) {
        Serilog.Log.Information($"TcpMpcClient: new connection from {socket.RemoteEndPoint}");

        // Create a PipeReader over the network stream
        using NetworkStream stream = new(socket);
        PipeReader reader = PipeReader.Create(stream);

        while (true) {
            if (cancellationToken.IsCancellationRequested) {
                break;
            }

            ReadResult result = await reader.ReadAsync(cancellationToken);
            ReadOnlySequence<byte> buffer = result.Buffer;

            Serilog.Log.Verbose($"TcpMpcClient: message received from {socket.RemoteEndPoint}; size {buffer.Length}");

            SequencePosition consumed;
            SequencePosition examined;
            while (this.TryGetPayload(buffer, out byte[]? payload, out consumed, out examined)) {
                Trace.Assert(payload is not null);

                ReadOnlySequence<byte> newBuffer = buffer.Slice(consumed);
                Trace.Assert(newBuffer.Length < buffer.Length);
                buffer = newBuffer;

                this.OnPayloadReceived?.Invoke(this, payload);
            }

            // Tell the PipeReader how much of the buffer has been consumed.
            reader.AdvanceTo(consumed, examined);

            // Stop reading if there's no more data coming.
            if (result.IsCompleted) {
                break;
            }
        }

        // Mark the PipeReader as complete.
        await reader.CompleteAsync();

        Serilog.Log.Information($"[{socket.RemoteEndPoint}]: disconnected");
    }

    private static readonly IReadOnlyList<byte> PayloadWrapStartIndicator = [0x11, 0x45, 0x14, 0x19, 0x19, 0x81, 0x00];

    private static void WrapPayload(byte[] payload, out byte[] wrapBuffer) {
        byte[] size = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(size, payload.Length);
        wrapBuffer = PayloadWrapStartIndicator.Concat(size).Concat(payload).ToArray();
    }

    private bool TryGetPayload(in ReadOnlySequence<byte> bufferReadOnlySequence, out byte[]? payload, out SequencePosition consumed, out SequencePosition examined) {
        if (bufferReadOnlySequence.Length < PayloadWrapStartIndicator.Count) {
            payload = null;
            consumed = bufferReadOnlySequence.Start;
            examined = bufferReadOnlySequence.End;
            return false;
        }

        byte[] buffer = bufferReadOnlySequence.ToArray();

        int startIndex = IReadOnlyListHelper.FindPattern(buffer, PayloadWrapStartIndicator);
        if (startIndex == -1) {
            payload = null;
            consumed = bufferReadOnlySequence.End;
            examined = bufferReadOnlySequence.End;
            return false;
        }

        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan()[(startIndex + PayloadWrapStartIndicator.Count)..]);
        if (payloadLength > buffer.Length - startIndex - PayloadWrapStartIndicator.Count - sizeof(int)) {
            payload = null;
            consumed = bufferReadOnlySequence.GetPosition(startIndex);
            examined = bufferReadOnlySequence.End;
            return false;
        }

        payload = buffer.AsSpan().Slice(startIndex + PayloadWrapStartIndicator.Count + sizeof(int), payloadLength).ToArray();
        consumed = examined = bufferReadOnlySequence.GetPosition(startIndex + PayloadWrapStartIndicator.Count + sizeof(int) + payloadLength);
        return true;
    }

    private async Task RunServerAsync(CancellationToken cancellationToken, IPEndPoint endpoint) {
        using Socket listenSocket = new(SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(endpoint);
        listenSocket.Listen();

        while (true) {
            cancellationToken.ThrowIfCancellationRequested();
            Socket socket = await listenSocket.AcceptAsync(cancellationToken);
            _ = this.HandleIncomingAsync(cancellationToken, socket);
        }
    }

    private Task? ServerTask { get; set; } = null;
    private CancellationTokenSource? CancellationTokenSource { get; set; } = null;

    public void Start(IPEndPoint endpoint) {
        if (this.ServerTask is not null) {
            throw new InvalidOperationException("TcpNetCilent is already started");
        }

        this.CancellationTokenSource = new CancellationTokenSource();
        this.ServerTask = Task.Run(() => this.RunServerAsync(this.CancellationTokenSource.Token, endpoint));
    }

    public void Stop() {
        if (this.ServerTask is not null) {
            Task serverTask = this.ServerTask;
            this.CancellationTokenSource?.Cancel();
            this.CancellationTokenSource = null;
            this.ServerTask = null;

            Serilog.Log.Information("Stopping TcpNetClient...");
            _ = Task.WaitAny(serverTask, Task.Delay(10000));
        }

        this.CancellationTokenSource = null;

        foreach (Socket socket in this.ClientSockets.Values) {
            socket.Close();
        }

        this.ClientSockets = [];
    }

    private async Task SendAsync(IPEndPoint endpoint, byte[] payload) {
        WrapPayload(payload, out byte[] wrapBuffer);

        // TODO: is it thread-safe?
        Socket clientSocket = await this.GetOrDialClientSocket(endpoint);
        try {
            _ = clientSocket.Send(wrapBuffer); // Note: send data as a whole. Don't call SendAsync twice (unless you have locked the socket; inefficient)
        }
        catch (ObjectDisposedException ex) {
            Serilog.Log.Warning(ex.Message, ex);
            throw;
        }
        catch (SocketException ex) {
            Serilog.Log.Warning(ex.Message, ex);
            throw;
        }
        catch (Exception ex) {
            Serilog.Log.Error(ex.Message, ex);
            throw;
        }
    }

    public void SendPayload(IPEndPoint endpoint, byte[] payload) => _ = this.SendAsync(endpoint, payload);
}
