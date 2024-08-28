using SadPencil.CompatCircuitCore.BinarySerialization;
using SadPencil.CompatCircuitCore.Extensions;
using System.Diagnostics;
using System.Numerics;

namespace SadPencil.CompatCircuitCore.Arithmetic;
/// <summary>
/// Field F_p
/// </summary>
public record Field : IBinaryEncodable, IArithFactoryBinaryDecodable<Field, Field> {
    public BigInteger Value { get; }
    public BigInteger FieldSize { get; }

    public Field(BigInteger value, BigInteger fieldSize) {
        if (value < 0 || value >= fieldSize) {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (fieldSize <= 1) {
            throw new ArgumentOutOfRangeException(nameof(fieldSize), "Field size must be a prime number (will not explicitly check for this!)");
        }

        this.Value = value;
        this.FieldSize = fieldSize;
    }

    private static void ThrowIfFieldSizeMismatch(Field a, Field b) {
        if (a.FieldSize != b.FieldSize) {
            throw new Exception($"FieldSize mismatch: {a.FieldSize} != {b.FieldSize}.");
        }
    }

    private Field NewField(BigInteger value) => new(value, this.FieldSize);

    public static Field operator +(Field a, Field b) {
        ThrowIfFieldSizeMismatch(a, b);
        return a.NewField((a.Value + b.Value) % a.FieldSize);
    }

    public static Field operator -(Field a, Field b) {
        ThrowIfFieldSizeMismatch(a, b);
        return a.NewField((a.FieldSize + a.Value - b.Value) % a.FieldSize);
    }

    public static Field operator *(Field a, Field b) {
        ThrowIfFieldSizeMismatch(a, b);
        return a.NewField(a.Value * b.Value % a.FieldSize);
    }

    public static Field operator -(Field a) => a.NewField((a.FieldSize - a.Value) % a.FieldSize);

    public Field Pow(BigInteger exponent) => this.NewField(BigInteger.ModPow(this.Value, exponent, this.FieldSize));

    public Field Pow(long exponent) => this.Pow(new BigInteger(exponent));

    public Field Pow(int exponent) => this.Pow(new BigInteger(exponent));

    public Field Inverse() => this.Value.IsZero ? throw new DivideByZeroException() : this.InverseOrZero();

    public Field InverseOrZero() => this.Pow(this.FieldSize - 2);

    public static Field operator /(Field a, Field b) => a * b.Inverse();

    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        this.Value.EncodeBytes(destination, out bytesWritten);
        Trace.Assert(bytesWritten == this.GetEncodedByteCount());
    }
    public int GetEncodedByteCount() => this.Value.GetEncodedByteCount();

    public static Field FromEncodedBytes(ReadOnlySpan<byte> buffer, IArithFactory<Field> factory, out int bytesRead) {
        BigInteger value = BigIntegerHelper.FromEncodedBytes(buffer, out bytesRead);
        return factory.New(value);
    }

    public sealed override string ToString() => this.Value.ToString();
    public static Field FromString(string value, BigInteger fieldSize) => new(BigInteger.Parse(value), fieldSize);

    public bool[] BitDecomposition() => this.Value.BitDecompositionUnsigned(Convert.ToInt32((this.FieldSize - 1).GetBitLength()));

    public static Field FromBitDecomposition(bool[] bits, BigInteger fieldSize) {
        BigInteger value = BigIntegerHelper.FromBitDecompositionUnsigned(bits);
        return new Field(value, fieldSize);
    }
}
