using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.BinarySerialization;

namespace Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives;
public class ArithFactoryBinaryDecodableFileEnumerator<T, TArithValue> : FileEnumeratorBase<T>, IEnumerator<T> where T : IArithFactoryBinaryDecodable<T, TArithValue>, IBinaryEncodable {
    public IArithFactory<TArithValue> Factory { get; }
    public ArithFactoryBinaryDecodableFileEnumerator(Stream stream, IArithFactory<TArithValue> factory) : base(stream) => this.Factory = factory;

    protected override T FromEncodedBytes(ReadOnlySpan<byte> buffer, out int bytesRead) {
        T obj = T.FromEncodedBytes(buffer, this.Factory, out int bytesRead_);
        bytesRead = bytesRead_;
        return obj;
    }
}
