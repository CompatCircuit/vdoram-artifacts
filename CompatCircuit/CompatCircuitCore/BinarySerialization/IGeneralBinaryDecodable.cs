namespace SadPencil.CompatCircuitCore.BinarySerialization;
public interface IGeneralBinaryDecodable<T> where T : IGeneralBinaryDecodable<T> {
    public static abstract T FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead);
}
