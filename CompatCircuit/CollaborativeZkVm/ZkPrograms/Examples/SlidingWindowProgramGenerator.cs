using Anonymous.CollaborativeZkVm.ZkVmCircuits;
using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;
using System.Numerics;

namespace Anonymous.CollaborativeZkVm.ZkPrograms.Examples;
public class SlidingWindowProgramGenerator : IZkProgramExampleGenerator {
    public ZkProgramExample GetZkProgram() {
        static Field NewField(BigInteger val) => ArithConfig.FieldFactory.New(val);
        static ZkProgramOpcode NewOp(ZkVmOpType opType, int? arg0 = null, int? arg1 = null, int? arg2 = null) => new(opType, NewField(arg0 ?? 0), NewField(arg1 ?? 0), NewField(arg2 ?? 0));
        string codeName = "SlidingWindowAlgorithm";
        string programName = "Sliding Window Algorithm";
        string programDescription = "Calculates the maximum sum of any k-length subarray using sliding window technique. Input array length n and window size k as public, then input n integer values as private. The program outputs the maximum subarray sum as public.";

        List<ZkProgramOpcode> opcodes = [
            NewOp(ZkVmOpType.Set, 7, 0),
            NewOp(ZkVmOpType.Set, 8, 1),
            NewOp(ZkVmOpType.PublicInput, 1),
            NewOp(ZkVmOpType.PublicInput, 2),
            NewOp(ZkVmOpType.LessThan, 9, 1, 2),
            NewOp(ZkVmOpType.Not, 9, 9),
            NewOp(ZkVmOpType.JumpIfZero, 9, 39),
            NewOp(ZkVmOpType.Set, 0, 0),
            NewOp(ZkVmOpType.LessThan, 6, 0, 1),
            NewOp(ZkVmOpType.JumpIfZero, 6, 14),
            NewOp(ZkVmOpType.PrivateInput, 3),
            NewOp(ZkVmOpType.Store, 3),
            NewOp(ZkVmOpType.Add, 0, 0, 8),
            NewOp(ZkVmOpType.JumpIfZero, 7, 8),
            NewOp(ZkVmOpType.Set, 3, 0),
            NewOp(ZkVmOpType.Set, 0, 0),
            NewOp(ZkVmOpType.LessThan, 6, 0, 2),
            NewOp(ZkVmOpType.JumpIfZero, 6, 22),
            NewOp(ZkVmOpType.Load, 4),
            NewOp(ZkVmOpType.Add, 3, 3, 4),
            NewOp(ZkVmOpType.Add, 0, 0, 8),
            NewOp(ZkVmOpType.JumpIfZero, 7, 16),
            NewOp(ZkVmOpType.Move, 4, 3),
            NewOp(ZkVmOpType.Move, 0, 2),
            NewOp(ZkVmOpType.LessThan, 9, 0, 1),
            NewOp(ZkVmOpType.JumpIfZero, 9, 37),
            NewOp(ZkVmOpType.Load, 5),
            NewOp(ZkVmOpType.Sub, 0, 0, 2),
            NewOp(ZkVmOpType.Load, 6),
            NewOp(ZkVmOpType.Add, 0, 0, 2),
            NewOp(ZkVmOpType.Sub, 3, 3, 6),
            NewOp(ZkVmOpType.Add, 3, 3, 5),
            NewOp(ZkVmOpType.LessThan, 9, 4, 3),
            NewOp(ZkVmOpType.JumpIfZero, 9, 35),
            NewOp(ZkVmOpType.Move, 4, 3),
            NewOp(ZkVmOpType.Add, 0, 0, 8),
            NewOp(ZkVmOpType.JumpIfZero, 7, 24),
            NewOp(ZkVmOpType.PublicOutput, 4),
            NewOp(ZkVmOpType.Halt),
            NewOp(ZkVmOpType.Set, 4, 0),
            NewOp(ZkVmOpType.PublicOutput, 4),
            NewOp(ZkVmOpType.Halt),
        ];

        List<Field> examplePublicInputs = new List<int> { 6, 3 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePrivateInputs = new List<int> { 5, 9, 3, 7, 2, 8 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePublicOutputs = new List<int> { 19 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        int globalStepsNoMoreThan = opcodes.Count * examplePrivateInputs.Count;

        return new ZkProgramExample() { CodeName = codeName, Name = programName, Description = programDescription, Opcodes = opcodes, ExamplePrivateInputs = examplePrivateInputs, ExamplePublicInputs = examplePublicInputs, ExamplePublicOutputs = examplePublicOutputs, GlobalStepsNoMoreThan = globalStepsNoMoreThan };
    }
}
