using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.GlobalConfig;
using SadPencil.CompatCircuitProgramming.CircuitElements;
using SadPencil.CompatCircuitProgramming.Gadgets;

namespace SadPencil.CompatCircuitProgrammingTest;
[TestClass]
public class FieldLessThanGadgetTest {
    [TestMethod]
    public void TestFieldLessThanGadget() {

        // Prepare circuit board
        CircuitBoard circuitBoard = new();

        Wire inLeftWire = Wire.NewPrivateInputWire("in_left");
        circuitBoard.AddWire(inLeftWire);

        Wire inRightWire = Wire.NewPrivateInputWire("in_right");
        circuitBoard.AddWire(inRightWire);

        GadgetInstance ins = new FieldLessThanGadget().ApplyGadget([inLeftWire, inRightWire], "less_than()");
        ins.Save(circuitBoard);
        Wire outputWire = ins.OutputWires[0];
        outputWire.Name = "out_is_less_than";
        outputWire.IsPublicOutput = true;

        circuitBoard = circuitBoard.Optimize();
        Serilog.Log.Information($"circuitBoard.Wires: {circuitBoard.WireCount}");
        Serilog.Log.Information($"circuitBoard.Operations: {circuitBoard.OperationCount}");

        // Execute circuit using SingleExecutor
        void TestLessThan2(Field left, Field right) {
            Field answer = ArithConfig.FieldFactory.New(left.Value < right.Value);

            CircuitBoardSingleExecutorWrapper executorWrapper = CircuitBoardSingleExecutorWrapper.FromNewSingleExecutor(circuitBoard, "FieldLessThanGadgetTestCircuit");
            executorWrapper.AddPrivate("in_left", left);
            executorWrapper.AddPrivate("in_right", right);

            _ = executorWrapper.Compute();

            Field evaluated = executorWrapper.GetOutput("out_is_less_than");
            Assert.AreEqual(answer, evaluated);
        }

        void TestLessThan1(Field value) {
            TestLessThan2(value, value);
            TestLessThan2(value, value - ArithConfig.FieldFactory.One);
            TestLessThan2(value, value + ArithConfig.FieldFactory.One);
            TestLessThan2(value, ArithConfig.FieldFactory.One);
            TestLessThan2(value, ArithConfig.FieldFactory.Zero);
            TestLessThan2(value, ArithConfig.FieldFactory.NegOne);
        }

        int testCount = 5;
        for (int testIndex = 0; testIndex < testCount; testIndex++) {
            Field left = ArithConfig.FieldFactory.Random();
            Field right = ArithConfig.FieldFactory.Random();
            TestLessThan2(left, right);
            TestLessThan1(left);
        }

        TestLessThan1(ArithConfig.FieldFactory.One);
        TestLessThan1(ArithConfig.FieldFactory.Zero);
        TestLessThan1(ArithConfig.FieldFactory.NegOne);
    }
}
