namespace Anonymous.CompatCircuitCore.BinarySerialization;
public interface IBinaryEncodable {
    public void EncodeBytes(Span<byte> destination, out int bytesWritten);
    public int GetEncodedByteCount();
}
