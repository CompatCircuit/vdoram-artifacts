using Anonymous.CompatCircuitCore.BinarySerialization;
using Anonymous.CompatCircuitCore.Extensions;
using System.Diagnostics;
using System.Numerics;

namespace Anonymous.CompatCircuitCore.Arithmetic;
/// <summary>
/// Modular ring Z_m
/// </summary>
public record Ring : IBinaryEncodable, IArithFactoryBinaryDecodable<Ring, Ring> {
    public BigInteger Value { get; }
    public BigInteger RingSize { get; }

    public Ring(BigInteger value, BigInteger ringSize) {
        if (value < 0 || value >= ringSize) {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (ringSize <= 1) {
            throw new ArgumentOutOfRangeException(nameof(ringSize), "The size of a ring (nontrivial) must be a positive number (excluding 1)");
        }

        this.Value = value;
        this.RingSize = ringSize;
    }

    private static void ThrowIfRingSizeMismatch(Ring a, Ring b) {
        if (a.RingSize != b.RingSize) {
            throw new Exception($"RingSize mismatch: {a.RingSize} != {b.RingSize}.");
        }
    }

    private Ring NewRing(BigInteger value) => new(value, this.RingSize);

    public static Ring operator +(Ring a, Ring b) {
        ThrowIfRingSizeMismatch(a, b);
        return a.NewRing((a.Value + b.Value) % a.RingSize);
    }

    public static Ring operator -(Ring a, Ring b) {
        ThrowIfRingSizeMismatch(a, b);
        return a.NewRing((a.RingSize + a.Value - b.Value) % a.RingSize);
    }
    public static Ring operator *(Ring a, Ring b) {
        ThrowIfRingSizeMismatch(a, b);
        return a.NewRing(a.Value * b.Value % a.RingSize);
    }

    public static Ring operator -(Ring a) => a.NewRing((a.RingSize - a.Value) % a.RingSize);

    public Ring Pow(BigInteger exponent) => this.NewRing(BigInteger.ModPow(this.Value, exponent, this.RingSize));
    public Ring Pow(long exponent) => this.Pow(new BigInteger(exponent));

    public Ring Pow(int exponent) => this.Pow(new BigInteger(exponent));

    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        this.Value.EncodeBytes(destination, out bytesWritten);
        Trace.Assert(bytesWritten == this.GetEncodedByteCount());
    }
    public int GetEncodedByteCount() => this.Value.GetEncodedByteCount();

    public static Ring FromEncodedBytes(ReadOnlySpan<byte> buffer, IArithFactory<Ring> factory, out int bytesRead) {
        BigInteger value = BigIntegerHelper.FromEncodedBytes(buffer, out bytesRead);
        return factory.New(value);
    }

    public sealed override string ToString() => this.Value.ToString();
    public static Ring FromString(string value, BigInteger ringSize) => new(BigInteger.Parse(value), ringSize);

    public bool[] BitDecomposition() => this.Value.BitDecompositionUnsigned(Convert.ToInt32((this.RingSize - 1).GetBitLength()));

    public static Ring FromBitDecomposition(bool[] bits, BigInteger ringSize) {
        BigInteger value = BigIntegerHelper.FromBitDecompositionUnsigned(bits);
        return new Ring(value, ringSize);
    }
}
