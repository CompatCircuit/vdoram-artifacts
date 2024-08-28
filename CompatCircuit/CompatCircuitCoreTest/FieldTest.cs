using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.GlobalConfig;

namespace SadPencil.CompatCircuitCoreTest;
[TestClass]
public class FieldTest {
    [TestMethod]
    public void TestFieldAddition() {

        Field a = ArithConfig.FieldFactory.Random();
        Field b = ArithConfig.FieldFactory.Random();
        Field c = ArithConfig.FieldFactory.Random();

        Field sum = a + b + c;
        Assert.IsTrue(sum - a == b + c);
        Assert.IsTrue(sum - b == c + a);
    }

    [TestMethod]
    public void TestFieldMultiplication() {

        Field a = ArithConfig.FieldFactory.RandomNonZero();
        Field b = ArithConfig.FieldFactory.RandomNonZero();
        Field c = ArithConfig.FieldFactory.RandomNonZero();

        Assert.AreEqual(ArithConfig.FieldFactory.One, b * b.InverseOrZero());

        Field prod = a * b * c;
        Assert.IsTrue(prod == c * a * b);

        Field cInv = c.InverseOrZero();
        Assert.IsTrue(a * b == prod * cInv);

        Assert.IsTrue(a * (b - b) == ArithConfig.FieldFactory.Zero);
    }

    [TestMethod]
    public void TestFieldInversion() {

        // This test method tests GetFieldSizeMinusTwo() defined in MPCExecutor.

        Field exp = ArithConfig.FieldFactory.NegOne - ArithConfig.FieldFactory.One;
        IReadOnlyList<bool> bits = exp.BitDecomposition();

        Field testBase = ArithConfig.FieldFactory.RandomNonZero();

        Field result = ArithConfig.FieldFactory.One;
        Field baseToCurrentPower = testBase;

        foreach (bool bit in bits) {
            if (bit) {
                result *= baseToCurrentPower;
            }
            baseToCurrentPower *= baseToCurrentPower;
        }

        Assert.AreEqual(result, testBase.Pow(ArithConfig.FieldSize - 2));
        Assert.IsTrue(result * testBase == ArithConfig.FieldFactory.One);
    }

    [TestMethod]
    public void TestBitDecomposition() {

        Field a = ArithConfig.FieldFactory.Random();
        bool[] bits = a.BitDecomposition();
        Assert.AreEqual(ArithConfig.BitSize, bits.Length);

        Field aRecovered = ArithConfig.FieldFactory.FromBitDecomposition(bits);
        Assert.AreEqual(a, aRecovered);
    }
}
