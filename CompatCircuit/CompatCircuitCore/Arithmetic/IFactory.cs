using System.Numerics;

namespace SadPencil.CompatCircuitCore.Arithmetic;
public interface IArithFactory<TArithValue> {
    public TArithValue New(BigInteger value);
}
