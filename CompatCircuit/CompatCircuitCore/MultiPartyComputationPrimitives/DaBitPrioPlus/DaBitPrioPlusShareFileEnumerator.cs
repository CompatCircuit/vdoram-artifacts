using Anonymous.CompatCircuitCore.Arithmetic;

namespace Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.DaBitPrioPlus;
public class DaBitPrioPlusShareFileEnumerator : ArithFactoryBinaryDecodableFileEnumerator<DaBitPrioPlusShare, Field> {
    public DaBitPrioPlusShareFileEnumerator(Stream stream, IArithFactory<Field> factory) : base(stream, factory) { }
}
