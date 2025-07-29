using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitProgramming.BitDecompositionProofCircuit;
using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CompatCircuitProgrammingTest;
[TestClass]
public class BitDecompositionProofCircuitBoardGeneratorTest {
    [TestMethod]
    public void TestBitDecompositionProofCircuitBoardGenerator() {

        CircuitBoard circuitBoard = new BitDecompositionProofCircuitBoardGenerator().GetCircuitBoard().Optimize();
        CircuitBoardSingleExecutorWrapper executorWrapper = CircuitBoardSingleExecutorWrapper.FromNewSingleExecutor(circuitBoard, "BitDecompositionProofCircuit");

        Field value = ArithConfig.FieldFactory.Random();
        bool[] bits = value.BitDecomposition();

        executorWrapper.AddPrivate("input_field", value);
        for (int i = 0; i < bits.Length; i++) {
            executorWrapper.AddPrivate($"input_bit_{i}", ArithConfig.FieldFactory.New(bits[i]));
        }

        _ = executorWrapper.Compute();

        // out_error_if_not_equals
        Field outErrorIfNotEquals = executorWrapper.GetOutput("out_error_if_not_equals");
        Assert.AreEqual(ArithConfig.FieldFactory.Zero, outErrorIfNotEquals);

        // out_error_if_input_bit_out_of_range
        Field outErrorIfInputBitOutOfRange = executorWrapper.GetOutput("out_error_if_input_bit_out_of_range");
        Assert.AreEqual(ArithConfig.FieldFactory.Zero, outErrorIfInputBitOutOfRange);

        // out_error_if_overflow
        Field outErrorIfOverflow = executorWrapper.GetOutput("out_error_if_overflow");
        Assert.AreEqual(ArithConfig.FieldFactory.Zero, outErrorIfOverflow);
    }
}
