using Anonymous.CompatCircuitCore.Arithmetic;

namespace Anonymous.CompatCircuitCore.BinarySerialization;
public interface IArithFactoryBinaryDecodable<T, TArithValue> where T : IArithFactoryBinaryDecodable<T, TArithValue> {
    public static abstract T FromEncodedBytes(ReadOnlySpan<byte> buffer, IArithFactory<TArithValue> factory, out int bytesRead);
}
