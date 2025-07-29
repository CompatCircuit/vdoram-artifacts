using Anonymous.CompatCircuitCore.Arithmetic;

namespace Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.EdaBitsKai;
public class EdaBitsKaiShareFileEnumerator : ArithFactoryBinaryDecodableFileEnumerator<EdaBitsKaiShare, Field> {
    public EdaBitsKaiShareFileEnumerator(Stream stream, IArithFactory<Field> factory) : base(stream, factory) { }
}
