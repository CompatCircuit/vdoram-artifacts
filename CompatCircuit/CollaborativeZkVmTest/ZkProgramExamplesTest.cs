using Anonymous.CollaborativeZkVm.ZkPrograms;
using Anonymous.CollaborativeZkVm.ZkPrograms.Examples;
using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.CompatCircuits.R1csCircuits;
using Anonymous.CompatCircuitCore.Computation;
using Anonymous.CompatCircuitCore.Computation.SingleParty;
using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitCore.GlobalConfig;

namespace Anonymous.CollaborativeZkVmTest;

[TestClass]
public class ZkProgramExamplesTest {
    private static async Task TestZkProgramWithMemoryTrace(
        ZkProgram program, IReadOnlyList<Field> publicInputs, IReadOnlyList<Field> privateInputs, IReadOnlyList<Field> expectedOutputs, int globalStepNoMoreThan) {

        ZkProgramInstance zkProgramInstance = new() {
            MyID = 0,
            PartyCount = 1,
            GlobalStepsNoMoreThan = globalStepNoMoreThan,
            Opcodes = program.Opcodes,
            PublicInputs = publicInputs,
            PrivateInputShares = privateInputs,
        };

        IMpcExecutorFactory mpcExecutorFactory = new SingleExecutorFactory();

        int r1csVerifyCount = 0;
        int expectedR1csVerifyCount = (2 * zkProgramInstance.Opcodes.Count) + 1; // TODO: make it abstract from ZkProgramExecutor implementation

        ZkProgramExecutor zkProgramExecutor = new() {
            ZkProgramInstance = zkProgramInstance,
            MyID = 0,
            MpcExecutorFactory = mpcExecutorFactory,
            IsSingleParty = true,
            OnR1csCircuitWithValuesGeneratedAsync = (string name, R1csCircuitWithValues r1cs) => {
                r1cs.SelfVerify();

                // There might be a time-consuming file writing operation. Simulate it.
                Thread.Sleep(TimeSpan.FromSeconds(4)); // 4 seconds.

                _ = Interlocked.Increment(ref r1csVerifyCount);
            },
        };

        ZkProgramExecuteResult result = await zkProgramExecutor.Execute();

        Serilog.Log.Information($"PublicOutputs: {string.Join(", ", result.PublicOutputs)}");
        Serilog.Log.Information($"GlobalStepCounter: {result.GlobalStepCounter}");

        Assert.AreEqual(expectedOutputs.Count, result.PublicOutputs.Count);
        Assert.IsTrue(expectedOutputs.SequenceEqual(result.PublicOutputs));
        Assert.AreEqual(expectedR1csVerifyCount, r1csVerifyCount);
    }

    [TestMethod]
    public async Task TestReverseInputZkProgram() {

        ZkProgram program = new ReverseInputProgramGenerator().GetZkProgram();
        List<Field> array = new List<int>() { 114, 514, 1919, 810, 114514 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> publicInputs = [ArithConfig.FieldFactory.New(array.Count)];

        List<Field> arrayReversed = array.ToList();
        arrayReversed.Reverse();

        await TestZkProgramWithMemoryTrace(program, publicInputs, privateInputs: array, expectedOutputs: arrayReversed, globalStepNoMoreThan: program.Opcodes.Count * array.Count);
    }

    [TestMethod]
    public async Task TestBubbleSortZkProgram() {

        ZkProgram program = new BubbleSortProgramGenerator().GetZkProgram();
        int inputLength = 4;
        // List<Field> array = new List<int>() { 114, 514, 1919, 810, 114514 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> array = Enumerable.Range(0, inputLength).Select(_ => ArithConfig.FieldFactory.Random()).ToList();
        List<Field> sortedArray = array.OrderBy(x => x.Value).ToList();
        List<Field> publicInputs = [ArithConfig.FieldFactory.New(array.Count)];
        await TestZkProgramWithMemoryTrace(program, publicInputs, privateInputs: array, expectedOutputs: sortedArray, globalStepNoMoreThan: program.Opcodes.Count * array.Count * array.Count);
    }

    [TestMethod]
    public async Task TestDivideByTwoZkProgram() {
        ZkProgram program = new DivideByTwoProgramGenerator().GetZkProgram();
        Field input = ArithConfig.FieldFactory.Random();
        Field expectedOutput = ArithConfig.FieldFactory.New(input.Value / 2);
        List<Field> publicInputs = [];
        List<Field> privateInputs = [input];
        List<Field> expectedOutputs = [expectedOutput];
        await TestZkProgramWithMemoryTrace(program, publicInputs, privateInputs, expectedOutputs, globalStepNoMoreThan: program.Opcodes.Count);
    }

    [TestMethod]
    public async Task TestGreaterThanZkProgram_Robust() {

        ZkProgram program = new GreaterThanZkProgramGenerator().GetZkProgram();

        async Task TestGreaterThan2(Field left, Field right) {
            List<Field> publicInputs = [];
            List<Field> privateInputs = [left, right];
            List<Field> expectedOutputs = [ArithConfig.FieldFactory.New(left.Value > right.Value)];

            await TestZkProgramWithMemoryTrace(program, publicInputs, privateInputs, expectedOutputs, globalStepNoMoreThan: program.Opcodes.Count);
        }

        async Task TestGreaterThan1(Field value) {
            await TestGreaterThan2(value, value);
            await TestGreaterThan2(value, value - ArithConfig.FieldFactory.One);
            await TestGreaterThan2(value, value + ArithConfig.FieldFactory.One);
            await TestGreaterThan2(value, ArithConfig.FieldFactory.One);
            await TestGreaterThan2(value, ArithConfig.FieldFactory.Zero);
            await TestGreaterThan2(value, ArithConfig.FieldFactory.NegOne);
        }

        int testCount = 1;
        for (int testIndex = 0; testIndex < testCount; testIndex++) {
            Field left = ArithConfig.FieldFactory.Random();
            Field right = ArithConfig.FieldFactory.Random();
            await TestGreaterThan2(left, right);
            await TestGreaterThan1(left);
        }

        await TestGreaterThan1(ArithConfig.FieldFactory.One);
        await TestGreaterThan1(ArithConfig.FieldFactory.Zero);
        await TestGreaterThan1(ArithConfig.FieldFactory.NegOne);
    }

    [TestMethod]
    public async Task TestGreaterThanZkProgram() {

        ZkProgram program = new GreaterThanZkProgramGenerator().GetZkProgram();

        async Task TestGreaterThan2(Field left, Field right) {
            List<Field> publicInputs = [];
            List<Field> privateInputs = [left, right];
            List<Field> expectedOutputs = [ArithConfig.FieldFactory.New(left.Value > right.Value)];

            await TestZkProgramWithMemoryTrace(program, publicInputs, privateInputs, expectedOutputs, globalStepNoMoreThan: program.Opcodes.Count);
        }

        Field left = ArithConfig.FieldFactory.Random();
        Field right = ArithConfig.FieldFactory.Random();
        await TestGreaterThan2(left, right);
    }
}
