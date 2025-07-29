using Anonymous.CompatCircuitCore.Arithmetic;

namespace Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
public class FieldBeaverTripleShareFileEnumerator : ArithFactoryBinaryDecodableFileEnumerator<FieldBeaverTripleShare, Field> {
    public FieldBeaverTripleShareFileEnumerator(Stream stream, IArithFactory<Field> factory) : base(stream, factory) { }
}
