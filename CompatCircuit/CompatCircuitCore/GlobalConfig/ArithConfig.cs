using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.SecretSharing;
using System.Globalization;
using System.Numerics;

namespace Anonymous.CompatCircuitCore.GlobalConfig;
public static class ArithConfig {
    private static BigInteger? _fieldSize = null;
    public static BigInteger FieldSize => _fieldSize ??= BigInteger.Parse("12ab655e9a2ca55660b44d1e5c37b00159aa76fed00000010a11800000000001".ToUpper(), NumberStyles.AllowHexSpecifier); // BLS12-377

    private static int? _bitSize = null;
    public static int BitSize => _bitSize ??= Convert.ToInt32((FieldSize - 1).GetBitLength()); // 253

    private static BigInteger? _fieldQuadraticNonresidue = null;
    public static BigInteger FieldQuadraticNonresidue => _fieldQuadraticNonresidue ??= 11; // such that FieldQuadraticNonresidue ^ {(FieldSize - 1) / 2} == -1

    private static FieldFactory? _fieldFactory = null;
    public static FieldFactory FieldFactory => _fieldFactory ??= new FieldFactory(FieldSize, RandomConfig.RandomGenerator);

    private static FieldSecretSharing? _fieldSecretSharing = null;
    public static FieldSecretSharing FieldSecretSharing => _fieldSecretSharing ??= new FieldSecretSharing() { FieldFactory = FieldFactory };

    private static RingFactory? _baseRingFactory = null;
    public static RingFactory BaseRingFactory => _baseRingFactory ??= new RingFactory(BigInteger.Pow(2, BitSize), RandomConfig.RandomGenerator);

    private static RingFactory? _extRingFactory = null;
    public static RingFactory ExtRingFactory => _extRingFactory ??= new RingFactory(BigInteger.Pow(2, BitSize + 1), RandomConfig.RandomGenerator);

    private static BoolSecretSharing? _boolSecretSharing = null;
    public static BoolSecretSharing BoolSecretSharing = _boolSecretSharing ??= new BoolSecretSharing() { RandomGenerator = RandomConfig.RandomGenerator };

    static ArithConfig() => JsonSerializerHelper.AddJsonConverter(new FieldJsonConverter(FieldSize));
    public static void Initialize() {
        // So that the static constructor is called
        // Seems ugly
    }
}
