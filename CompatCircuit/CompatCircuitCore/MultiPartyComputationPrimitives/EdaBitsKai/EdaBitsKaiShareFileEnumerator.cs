using SadPencil.CompatCircuitCore.Arithmetic;

namespace SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.EdaBitsKai;
public class EdaBitsKaiShareFileEnumerator : ArithFactoryBinaryDecodableFileEnumerator<EdaBitsKaiShare, Field> {
    public EdaBitsKaiShareFileEnumerator(Stream stream, IArithFactory<Field> factory) : base(stream, factory) { }
}
