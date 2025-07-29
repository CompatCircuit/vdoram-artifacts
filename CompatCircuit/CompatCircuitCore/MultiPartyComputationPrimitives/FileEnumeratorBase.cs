using Anonymous.CompatCircuitCore.BinarySerialization;
using Anonymous.CompatCircuitCore.Extensions;
using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives;
public abstract class FileEnumeratorBase<T> : IDisposable, ICountingEnumerator<T> where T : IBinaryEncodable {
    protected const byte HaveNext = 114;
    public static void AppendToStream(Stream stream, T value) {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(value);

        Trace.Assert(stream.Position == stream.Length);
        //_ = stream.Seek(0, SeekOrigin.End);

        int byteCount = value.GetEncodedByteCount();
        byte[] buffer = new byte[byteCount];
        {
            value.EncodeBytes(buffer, out int bytesWritten);
            Trace.Assert(byteCount == bytesWritten);
        }

        // Write 'HaveNext'
        stream.WriteByte(HaveNext);

        // Write byte count
        {
            byte[] intBuffer = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(intBuffer, byteCount);
            stream.Write(intBuffer, 0, sizeof(int));
        }

        // Write content
        stream.Write(buffer, 0, byteCount);
    }

    public static void WriteStream(Stream stream, IEnumerable<T> values, bool leaveOpen = false) {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(values);

        foreach (T value in values) {
            AppendToStream(stream, value);
        }

        stream.Flush();
        if (!leaveOpen) {
            stream.Close();
        }
    }

    public Stream Stream { get; }
    public long Count { get; private set; } = 0;
    public bool LeaveOpen { get; set; } = false;

    private bool _currentValid = false;
    private T? _current;
    public T Current => this._currentValid ? this._current! : throw new InvalidOperationException();

    object IEnumerator.Current => this.Current;

    public FileEnumeratorBase(Stream stream) => this.Stream = stream;
    ~FileEnumeratorBase() { this.Dispose(); }
    public void Dispose() {
        if (!this.LeaveOpen) {
            this.Stream?.Close();
        }
    }

    protected abstract T FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead);

    public bool MoveNext() {
        int haveNext = this.Stream.ReadByte();
        switch (haveNext) {
            case -1:
                this._currentValid = false;
                return false;
            case HaveNext: {
                    // Read byte count
                    int byteCount;
                    {
                        byte[] intBuffer = new byte[sizeof(int)];
                        this.Stream.ReadExactly(intBuffer);
                        byteCount = BinaryPrimitives.ReadInt32LittleEndian(intBuffer);
                    }

                    byte[] buffer = new byte[byteCount];
                    this.Stream.ReadExactly(buffer);
                    T currentObj;
                    {
                        currentObj = this.FromEncodedBytes(buffer, out int bytesRead);
                        Trace.Assert(bytesRead == byteCount);
                    }

                    this._current = currentObj;
                    this._currentValid = true;
                    this.Count++;
                }
                return true;
            default:
                this._currentValid = false;
                throw new InvalidDataException($"Unexpected byte at position {this.Stream.Position}");
        }
    }
    public void Reset() {
        _ = this.Stream.Seek(0, SeekOrigin.Begin);
        this.Count = 0;
        this._current = default;
    }
}
