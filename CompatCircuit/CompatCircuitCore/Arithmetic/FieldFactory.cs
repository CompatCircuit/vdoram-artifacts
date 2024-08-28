using SadPencil.CompatCircuitCore.RandomGenerators;
using System.Numerics;

namespace SadPencil.CompatCircuitCore.Arithmetic;
public record FieldFactory : IArithFactory<Field> {
    public BigInteger FieldSize { get; }

    public Field Zero { get; }
    public Field One { get; }
    public Field NegOne { get; }
    public Field Two { get; }

    public RandomGeneratorRef RandomGenerator { get; }

    public FieldFactory(BigInteger fieldSize, RandomGeneratorRef randomGenerator) {
        if (fieldSize <= 1 || (fieldSize > 2 && fieldSize % 2 == 0)) {
            throw new ArgumentOutOfRangeException(nameof(fieldSize), "Field size must be a prime number (will not explicitly check for this!)");
        }

        this.FieldSize = fieldSize;

        this.Zero = this.New(0);
        this.One = this.New(1);
        this.NegOne = this.New(this.FieldSize - 1);
        this.Two = this.New(2);

        this.RandomGenerator = randomGenerator;
    }

    public Field New(bool bit) => bit ? this.One : this.Zero;
    public Field New(BigInteger value) => new(value, this.FieldSize);

    public Field Random() => this.New(RandomHelper.RandomBelow(this.FieldSize, this.RandomGenerator));

    public Field RandomNonZero() => this.One + this.New(RandomHelper.RandomBelow(this.FieldSize - 1, this.RandomGenerator));

    public Field FromString(string value) => Field.FromString(value, this.FieldSize);

    public Field FromBitDecomposition(bool[] bits) => Field.FromBitDecomposition(bits, this.FieldSize);
}
