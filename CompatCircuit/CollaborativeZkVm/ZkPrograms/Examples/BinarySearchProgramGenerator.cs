using Anonymous.CollaborativeZkVm.ZkVmCircuits;
using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;
using System.Numerics;

namespace Anonymous.CollaborativeZkVm.ZkPrograms.Examples;
public class BinarySearchProgramGenerator : IZkProgramExampleGenerator {
    public ZkProgramExample GetZkProgram() {
        static Field NewField(BigInteger val) => ArithConfig.FieldFactory.New(val);
        static ZkProgramOpcode NewOp(ZkVmOpType opType, int? arg0 = null, int? arg1 = null, int? arg2 = null) => new(opType, NewField(arg0 ?? 0), NewField(arg1 ?? 0), NewField(arg2 ?? 0));
        string codeName = "BinarySearch";
        string programName = "Binary Search";
        string programDescription = "Searches for a target value in a sorted array using the binary search algorithm. Input the array length n as public, then input the target value followed by n sorted array values as private. The program outputs the 1-based index position if the target is found (where 1 = first element), or 0 if not found.";
        List<ZkProgramOpcode> opcodes = [
            NewOp(ZkVmOpType.Set, 7, 1),
            NewOp(ZkVmOpType.Set, 8, 0),
            NewOp(ZkVmOpType.Set, 10, 0),
            NewOp(ZkVmOpType.PublicInput, 1),
            NewOp(ZkVmOpType.PrivateInput, 11),
            NewOp(ZkVmOpType.Set, 2, 0),
            NewOp(ZkVmOpType.LessThan, 6, 2, 1),
            NewOp(ZkVmOpType.JumpIfZero, 6, 13),
            NewOp(ZkVmOpType.PrivateInput, 5),
            NewOp(ZkVmOpType.Move, 0, 2),
            NewOp(ZkVmOpType.Store, 5),
            NewOp(ZkVmOpType.Add, 2, 2, 7),
            NewOp(ZkVmOpType.JumpIfZero, 8, 6),
            NewOp(ZkVmOpType.Set, 2, 0),
            NewOp(ZkVmOpType.Sub, 3, 1, 7),
            NewOp(ZkVmOpType.Sub, 5, 3, 2),
            NewOp(ZkVmOpType.LessThan, 6, 5, 8),
            NewOp(ZkVmOpType.Not, 6, 6),
            NewOp(ZkVmOpType.JumpIfZero, 6, 33),
            NewOp(ZkVmOpType.Add, 4, 2, 3),
            NewOp(ZkVmOpType.RightShift,4, 4),
            NewOp(ZkVmOpType.Move, 0, 4),
            NewOp(ZkVmOpType.Load, 5),
            NewOp(ZkVmOpType.Sub, 6, 5, 11),
            NewOp(ZkVmOpType.JumpIfZero, 6, 31),
            NewOp(ZkVmOpType.LessThan, 9, 6, 8),
            NewOp(ZkVmOpType.JumpIfZero, 9, 29),
            NewOp(ZkVmOpType.Add, 2, 4, 7),
            NewOp(ZkVmOpType.JumpIfZero, 8, 15),
            NewOp(ZkVmOpType.Sub, 3, 4, 7),
            NewOp(ZkVmOpType.JumpIfZero, 8, 16),
            NewOp(ZkVmOpType.Move, 10, 4),
            NewOp(ZkVmOpType.Add, 10, 10, 7),
            NewOp(ZkVmOpType.PublicOutput, 10),
            NewOp(ZkVmOpType.Halt)
        ];

        List<Field> examplePublicInputs = new List<int> { 6 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePrivateInputs = new List<int> { 23, 8, 12, 16, 23, 38, 42 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePublicOutputs = new List<int> { 4 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        int globalStepsNoMoreThan = opcodes.Count * examplePrivateInputs.Count * examplePrivateInputs.Count;

        return new ZkProgramExample() { CodeName = codeName, Name = programName, Description = programDescription, Opcodes = opcodes, ExamplePrivateInputs = examplePrivateInputs, ExamplePublicInputs = examplePublicInputs, ExamplePublicOutputs = examplePublicOutputs, GlobalStepsNoMoreThan = globalStepsNoMoreThan };
    }
}
