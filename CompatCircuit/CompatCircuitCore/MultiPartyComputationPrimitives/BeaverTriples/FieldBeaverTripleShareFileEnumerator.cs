using SadPencil.CompatCircuitCore.Arithmetic;

namespace SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
public class FieldBeaverTripleShareFileEnumerator : ArithFactoryBinaryDecodableFileEnumerator<FieldBeaverTripleShare, Field> {
    public FieldBeaverTripleShareFileEnumerator(Stream stream, IArithFactory<Field> factory) : base(stream, factory) { }
}
