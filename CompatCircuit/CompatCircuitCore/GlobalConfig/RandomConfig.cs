using Anonymous.CompatCircuitCore.RandomGenerators;

namespace Anonymous.CompatCircuitCore.GlobalConfig;
public static class RandomConfig {
    public static RandomGeneratorRef RandomGenerator { get; } = new RandomGeneratorRef() { Value = new CryptographyRandomGenerator() };
}
