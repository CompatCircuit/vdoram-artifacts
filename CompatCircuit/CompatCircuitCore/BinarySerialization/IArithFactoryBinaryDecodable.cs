using SadPencil.CompatCircuitCore.Arithmetic;

namespace SadPencil.CompatCircuitCore.BinarySerialization;
public interface IArithFactoryBinaryDecodable<T, TArithValue> where T : IArithFactoryBinaryDecodable<T, TArithValue> {
    public static abstract T FromEncodedBytes(ReadOnlySpan<byte> buffer, IArithFactory<TArithValue> factory, out int bytesRead);
}
