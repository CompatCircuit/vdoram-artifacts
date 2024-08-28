using SadPencil.CompatCircuitCore.Arithmetic;

namespace SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.DaBitPrioPlus;
public class DaBitPrioPlusShareFileEnumerator : ArithFactoryBinaryDecodableFileEnumerator<DaBitPrioPlusShare, Field> {
    public DaBitPrioPlusShareFileEnumerator(Stream stream, IArithFactory<Field> factory) : base(stream, factory) { }
}
