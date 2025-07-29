using Anonymous.CompatCircuitCore.Extensions;
using System.Collections;
using System.Numerics;

namespace Anonymous.CompatCircuitCore.RandomGenerators;
public static class RandomHelper {
    public static BitArray RandomBits(int bitCount, RandomGeneratorRef random) => RandomBits(bitCount, random.Value);
    public static BitArray RandomBits(int bitCount, IRandomGenerator random) {
        byte[] randomBuffer = new byte[BitArrayHelper.BitCountToByteCount(bitCount)];
        random.Fill(randomBuffer);

        return new BitArray(randomBuffer) { Length = bitCount };
    }
    public static BigInteger RandomBelow(BigInteger below, RandomGeneratorRef random) => RandomBelow(below, random.Value);
    public static BigInteger RandomBelow(BigInteger below, IRandomGenerator random) {
        if (below <= 0) {
            throw new ArgumentOutOfRangeException(nameof(below), "Argument must be positive.");
        }

        int bytesNeeded = below.GetByteCount(isUnsigned: true);
        byte[] bytes = new byte[bytesNeeded];

        while (true) {
            random.Fill(bytes);

            BigInteger value = new(bytes, isUnsigned: true, isBigEndian: false);

            // Check if within range, retry if not. This could theoretically loop for a long time,
            // but statistically, it's very unlikely for suitably sized ranges.
            if (value >= 0 && value < below) {
                return value;
            }
        }
    }
}
