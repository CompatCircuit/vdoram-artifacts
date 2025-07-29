using Anonymous.CollaborativeZkVm.ZkVmCircuits;
using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;
using System.Numerics;

namespace Anonymous.CollaborativeZkVm.ZkPrograms.Examples;
public class LongestContinuousIncreasingSubsequenceProgramGenerator : IZkProgramExampleGenerator {
    public ZkProgramExample GetZkProgram() {
        static Field NewField(BigInteger val) => ArithConfig.FieldFactory.New(val);
        static ZkProgramOpcode NewOp(ZkVmOpType opType, int? arg0 = null, int? arg1 = null, int? arg2 = null) => new(opType, NewField(arg0 ?? 0), NewField(arg1 ?? 0), NewField(arg2 ?? 0));
        string codeName = "LongestContinuousIncreasingSubsequence";
        string programName = "Longest Continuous Increasing Subsequence";
        string programDescription = "Finds the length of the longest increasing subsequence in an array. Input the array length n as public, then input n integer values as private. The program outputs the maximum increasing subsequence length as public.";
        List<ZkProgramOpcode> opcodes = [
            NewOp(ZkVmOpType.Set, 7, 0),
            NewOp(ZkVmOpType.Set, 8, 1),
            NewOp(ZkVmOpType.PublicInput, 1),
            NewOp(ZkVmOpType.LessThan, 6, 7, 1),
            NewOp(ZkVmOpType.JumpIfZero, 6, 25),
            NewOp(ZkVmOpType.PrivateInput, 2),
            NewOp(ZkVmOpType.Set, 4, 1),
            NewOp(ZkVmOpType.Set, 5, 1),
            NewOp(ZkVmOpType.Set, 0, 1),
            NewOp(ZkVmOpType.LessThan, 6, 0, 1),
            NewOp(ZkVmOpType.JumpIfZero, 6, 23),
            NewOp(ZkVmOpType.PrivateInput, 3),
            NewOp(ZkVmOpType.LessThan, 6, 2, 3),
            NewOp(ZkVmOpType.JumpIfZero, 6, 21),
            NewOp(ZkVmOpType.Add, 5, 5, 8),
            NewOp(ZkVmOpType.LessThan, 6, 4, 5),
            NewOp(ZkVmOpType.JumpIfZero, 6, 17),
            NewOp(ZkVmOpType.Move, 4, 5),
            NewOp(ZkVmOpType.Move, 2, 3),
            NewOp(ZkVmOpType.Add, 0, 0, 8),
            NewOp(ZkVmOpType.JumpIfZero, 7, 9),
            NewOp(ZkVmOpType.Set, 5, 1),
            NewOp(ZkVmOpType.JumpIfZero, 7, 18),
            NewOp(ZkVmOpType.PublicOutput, 4),
            NewOp(ZkVmOpType.Halt),
            NewOp(ZkVmOpType.Set, 4, 0),
            NewOp(ZkVmOpType.PublicOutput, 4),
            NewOp(ZkVmOpType.Halt),
        ];

        List<Field> examplePublicInputs = new List<int> { 5 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePrivateInputs = new List<int> { 1, 2, 3, 4, 1 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePublicOutputs = new List<int> { 4 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        int globalStepsNoMoreThan = 300;

        return new ZkProgramExample() { CodeName = codeName, Name = programName, Description = programDescription, Opcodes = opcodes, ExamplePrivateInputs = examplePrivateInputs, ExamplePublicInputs = examplePublicInputs, ExamplePublicOutputs = examplePublicOutputs, GlobalStepsNoMoreThan = globalStepsNoMoreThan };
    }
}
