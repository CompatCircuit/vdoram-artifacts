using Anonymous.CollaborativeZkVm.ZkVmCircuits;
using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;
using System.Numerics;

namespace Anonymous.CollaborativeZkVm.ZkPrograms.Examples;
public class SetIntersectionProgramGenerator : IZkProgramExampleGenerator {
    public ZkProgramExample GetZkProgram() {
        static Field NewField(BigInteger val) => ArithConfig.FieldFactory.New(val);
        static ZkProgramOpcode NewOp(ZkVmOpType opType, int? arg0 = null, int? arg1 = null, int? arg2 = null) => new(opType, NewField(arg0 ?? 0), NewField(arg1 ?? 0), NewField(arg2 ?? 0));
        string codeName = "SetIntersection";
        string programName = "Set Intersection";
        string programDescription = "Computes the size of intersection between two sorted arrays using a two-pointer technique. Public inputs are the sizes of the arrays, private inputs are the array elements. The program outputs the size of the intersection.";
        List<ZkProgramOpcode> opcodes = [
            NewOp(ZkVmOpType.Set, 10, 1),
            NewOp(ZkVmOpType.Set, 11, 0),
            NewOp(ZkVmOpType.PublicInput, 1),
            NewOp(ZkVmOpType.PublicInput, 2),
            NewOp(ZkVmOpType.Set, 3, 0),
            NewOp(ZkVmOpType.LessThan, 4, 3, 1),
            NewOp(ZkVmOpType.JumpIfZero, 4, 12),
            NewOp(ZkVmOpType.PrivateInput, 5),
            NewOp(ZkVmOpType.Move, 0, 3),
            NewOp(ZkVmOpType.Store, 5),
            NewOp(ZkVmOpType.Add, 3, 3, 10),
            NewOp(ZkVmOpType.JumpIfZero, 11, 5),
            NewOp(ZkVmOpType.Set, 4, 0),
            NewOp(ZkVmOpType.LessThan, 5, 4, 2),
            NewOp(ZkVmOpType.JumpIfZero, 5, 20),
            NewOp(ZkVmOpType.PrivateInput, 6),
            NewOp(ZkVmOpType.Add, 0, 1, 4),
            NewOp(ZkVmOpType.Store, 6),
            NewOp(ZkVmOpType.Add, 4, 4, 10),
            NewOp(ZkVmOpType.JumpIfZero, 11, 13),
            NewOp(ZkVmOpType.Set, 6, 0),
            NewOp(ZkVmOpType.Set, 7, 0),
            NewOp(ZkVmOpType.Set, 8, 0),
            NewOp(ZkVmOpType.LessThan, 9, 6, 1),
            NewOp(ZkVmOpType.LessThan, 12, 7, 2),
            NewOp(ZkVmOpType.And, 13, 9, 12),
            NewOp(ZkVmOpType.JumpIfZero, 13, 44),
            NewOp(ZkVmOpType.Move, 0, 6),
            NewOp(ZkVmOpType.Load, 14),
            NewOp(ZkVmOpType.Add, 0, 1, 7),
            NewOp(ZkVmOpType.Load, 15),
            NewOp(ZkVmOpType.Sub, 13, 14, 15),
            NewOp(ZkVmOpType.LessThan, 14, 13, 11),
            NewOp(ZkVmOpType.JumpIfZero, 14, 36),
            NewOp(ZkVmOpType.Add, 6, 6, 10),
            NewOp(ZkVmOpType.JumpIfZero, 11, 23),
            NewOp(ZkVmOpType.Norm, 15, 13),
            NewOp(ZkVmOpType.Not, 15, 15),
            NewOp(ZkVmOpType.JumpIfZero, 15, 42),
            NewOp(ZkVmOpType.Add, 8, 8, 10),
            NewOp(ZkVmOpType.Add, 6, 6, 10),
            NewOp(ZkVmOpType.JumpIfZero, 11, 23),
            NewOp(ZkVmOpType.Add, 7, 7, 10),
            NewOp(ZkVmOpType.JumpIfZero, 11, 23),
            NewOp(ZkVmOpType.PublicOutput, 8),
            NewOp(ZkVmOpType.Halt)
        ];

        List<Field> examplePublicInputs = new List<int> { 6, 4 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePrivateInputs = new List<int> { 2, 3, 4, 6, 7, 10, 2, 3, 6, 8 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePublicOutputs = new List<int> { 3 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        int globalStepsNoMoreThan = opcodes.Count * examplePrivateInputs.Count * examplePrivateInputs.Count;

        return new ZkProgramExample() { CodeName = codeName, Name = programName, Description = programDescription, Opcodes = opcodes, ExamplePrivateInputs = examplePrivateInputs, ExamplePublicInputs = examplePublicInputs, ExamplePublicOutputs = examplePublicOutputs, GlobalStepsNoMoreThan = globalStepsNoMoreThan };
    }
}
