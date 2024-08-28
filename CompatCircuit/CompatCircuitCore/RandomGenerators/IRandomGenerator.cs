namespace SadPencil.CompatCircuitCore.RandomGenerators;

public interface IRandomGenerator {
    public void Fill(Span<byte> data);
}