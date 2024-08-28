using System.Security.Cryptography;

namespace SadPencil.CompatCircuitCore.RandomGenerators;
public class CryptographyRandomGenerator : IRandomGenerator, IRandomGeneratorSingleton {
    public static void Fill(Span<byte> data) => RandomNumberGenerator.Fill(data);
    void IRandomGenerator.Fill(Span<byte> data) => Fill(data);
}
