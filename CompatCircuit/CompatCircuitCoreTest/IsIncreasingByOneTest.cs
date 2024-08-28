using SadPencil.CompatCircuitCore.Extensions;

namespace SadPencil.CompatCircuitCoreTest;

[TestClass]
public class IsIncreasingByOneTest {
    [TestMethod]
    public void Test_IsEmptySequence_ReturnsTrue() {

        // An empty sequence technically does not violate the increasing-by-one rule.
        IEnumerable<int> numbers = [];
        bool result = numbers.IsIncreasingByOne();
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Test_IsIncreasingByOne_ReturnsTrue() {

        IEnumerable<int> numbers = [1, 2, 3, 4, 5];
        bool result = numbers.IsIncreasingByOne();
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Test_IncreaseByMoreThanOne_ReturnsFalse() {

        IEnumerable<int> numbers = [1, 2, 4, 5];
        bool result = numbers.IsIncreasingByOne();
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Test_ContainsNegativeNumbersIncreasingByOne_ReturnsTrue() {

        IEnumerable<int> numbers = [-3, -2, -1, 0, 1];
        bool result = numbers.IsIncreasingByOne();
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Test_SequenceDecreases_ReturnsFalse() {

        IEnumerable<int> numbers = [5, 4, 3, 2, 1];
        bool result = numbers.IsIncreasingByOne();
        Assert.IsFalse(result);
    }
}
