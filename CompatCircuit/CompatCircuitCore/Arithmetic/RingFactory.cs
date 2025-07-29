using Anonymous.CompatCircuitCore.RandomGenerators;
using System.Numerics;

namespace Anonymous.CompatCircuitCore.Arithmetic;
public record RingFactory : IArithFactory<Ring> {
    public BigInteger RingSize { get; }

    public Ring Zero { get; }
    public Ring One { get; }
    public Ring NegOne { get; }
    public Ring Two { get; }
    public RandomGeneratorRef RandomGenerator { get; }

    public RingFactory(BigInteger ringSize, RandomGeneratorRef randomGenerator) {
        if (ringSize <= 1) {
            throw new ArgumentOutOfRangeException(nameof(ringSize), "The size of a ring (nontrivial) must be a positive number (excluding 1)");
        }

        this.RingSize = ringSize;

        this.Zero = this.New(0);
        this.One = this.New(1);
        this.NegOne = this.New(this.RingSize - 1);
        this.Two = this.New(2);

        this.RandomGenerator = randomGenerator;
    }

    public Ring New(bool bit) => bit ? this.One : this.Zero;
    public Ring New(BigInteger value) => new(value, this.RingSize);

    public Ring NewTruncate(BigInteger value) => this.New(value % this.RingSize);

    public Ring Random() => this.New(RandomHelper.RandomBelow(this.RingSize, this.RandomGenerator));

    public Ring RandomNonZero() => this.One + this.New(RandomHelper.RandomBelow(this.RingSize - 1, this.RandomGenerator));

    public Ring FromString(string value) => Ring.FromString(value, this.RingSize);
    public Ring FromBitDecomposition(bool[] bits) => Ring.FromBitDecomposition(bits, this.RingSize);
}

