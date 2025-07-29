using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;

namespace Anonymous.CompatCircuitCoreTest;
[TestClass]
public class RingTest {
    public static RingFactory RingFactory { get; } = ArithConfig.BaseRingFactory;

    [TestMethod]
    public void TestRingAddition() {

        Ring a = RingFactory.Random();
        Ring b = RingFactory.Random();
        Ring c = RingFactory.Random();

        Ring sum = a + b + c;
        Assert.IsTrue(sum - a == b + c);
        Assert.IsTrue(sum - b == c + a);
    }

    [TestMethod]
    public void TestRingMultiplication() {

        Ring a = RingFactory.RandomNonZero();
        Ring b = RingFactory.RandomNonZero();
        Ring c = RingFactory.RandomNonZero();

        Ring prod = a * b * c;
        Assert.IsTrue(prod == c * a * b);

        Assert.IsTrue(a * (b - b) == RingFactory.Zero);
    }

    [TestMethod]
    public void TestBitDecomposition() {

        Ring a = RingFactory.Random();
        bool[] bits = a.BitDecomposition();
        Assert.AreEqual(ArithConfig.BitSize, bits.Length);

        Ring aRecovered = RingFactory.FromBitDecomposition(bits);
        Assert.AreEqual(a, aRecovered);
    }
}
