using System.Numerics;

namespace Anonymous.CompatCircuitCore.Arithmetic;
public interface IArithFactory<TArithValue> {
    public TArithValue New(BigInteger value);
}
