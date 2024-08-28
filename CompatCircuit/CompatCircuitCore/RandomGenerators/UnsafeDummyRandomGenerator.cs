namespace SadPencil.CompatCircuitCore.RandomGenerators;

/// <summary>
/// This random generator fills target location with repeated pattern, as if they were random.<br/>
/// Warning: only use it for performance evaluation. Do not use it in unit test or in production.
/// </summary>
public class UnsafeDummyRandomGenerator : IRandomGenerator, IRandomGeneratorSingleton {
    public static ReadOnlyMemory<byte> DummyBytes = new([19, 19, 81, 01, 14, 51, 40]);

    private static void CopyToSpan(ReadOnlySpan<byte> source, Span<byte> target) {
        int sourceLength = source.Length;
        int offset = 0;

        while (target.Length - offset > sourceLength) {
            // Copy the entire source until the remaining target space is less than sourceLength
            source.CopyTo(target.Slice(offset, sourceLength));
            offset += sourceLength;
        }

        // Handle any remaining space in the target which is less than a full sourceLength
        source[..(target.Length - offset)].CopyTo(target[offset..]);
    }

    public static void Fill(Span<byte> data) => CopyToSpan(DummyBytes.Span, data);

    void IRandomGenerator.Fill(Span<byte> data) => Fill(data);
}
