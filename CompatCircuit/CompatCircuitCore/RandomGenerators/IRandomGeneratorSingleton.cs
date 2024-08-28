namespace SadPencil.CompatCircuitCore.RandomGenerators;
public interface IRandomGeneratorSingleton {
    public static abstract void Fill(Span<byte> data);
}
