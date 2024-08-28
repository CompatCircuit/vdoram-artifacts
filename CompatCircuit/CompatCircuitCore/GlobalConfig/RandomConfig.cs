using SadPencil.CompatCircuitCore.RandomGenerators;

namespace SadPencil.CompatCircuitCore.GlobalConfig;
public static class RandomConfig {
    public static RandomGeneratorRef RandomGenerator { get; } = new RandomGeneratorRef() { Value = new CryptographyRandomGenerator() };
}
