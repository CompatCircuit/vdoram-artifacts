namespace Anonymous.CompatCircuitCore.RandomGenerators;
public class UnsafeSystemRandomGenerator : IRandomGenerator {
    public Random Random { get; init; } = new Random();
    public void Fill(Span<byte> data) => this.Random.NextBytes(data);
}
