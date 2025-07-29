using Anonymous.CollaborativeZkVm.ZkVmCircuits;
using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;
using System.Numerics;

namespace Anonymous.CollaborativeZkVm.ZkPrograms.Examples;
public class FibonacciProgramGenerator : IZkProgramExampleGenerator {
    public ZkProgramExample GetZkProgram() {
        static Field NewField(BigInteger val) => ArithConfig.FieldFactory.New(val);
        static ZkProgramOpcode NewOp(ZkVmOpType opType, int? arg0 = null, int? arg1 = null, int? arg2 = null) => new(opType, NewField(arg0 ?? 0), NewField(arg1 ?? 0), NewField(arg2 ?? 0));
        string codeName = "FibonacciNumberAlgorithm";
        string programName = "Fibonacci Number Algorithm";
        string programDescription = "Computes the nth Fibonacci number and outputs its value for a given non-negative integer n.";
        List<ZkProgramOpcode> opcodes = [
            NewOp(ZkVmOpType.PrivateInput, 0),
            NewOp(ZkVmOpType.Set, 1, 0),
            NewOp(ZkVmOpType.Set, 2, 1),
            NewOp(ZkVmOpType.Norm, 8, 0),
            NewOp(ZkVmOpType.JumpIfZero, 0, 15),
            NewOp(ZkVmOpType.Set, 5, 1),
            NewOp(ZkVmOpType.Set, 6, 1),
            NewOp(ZkVmOpType.LessThan, 7, 5, 0),
            NewOp(ZkVmOpType.JumpIfZero, 7, 14),
            NewOp(ZkVmOpType.Add, 3, 1, 2),
            NewOp(ZkVmOpType.Move, 1, 2),
            NewOp(ZkVmOpType.Move, 2, 3),
            NewOp(ZkVmOpType.Add, 5, 5, 6),
            NewOp(ZkVmOpType.JumpIfZero, 4, 6),
            NewOp(ZkVmOpType.Move, 8, 2),
            NewOp(ZkVmOpType.PublicOutput, 8),
            NewOp(ZkVmOpType.Halt),
        ];

        List<Field> examplePublicInputs = [];
        List<Field> examplePrivateInputs = new List<int> { 10 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePublicOutputs = new List<int> { 55 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        int globalStepsNoMoreThan = 1000;

        return new ZkProgramExample() { CodeName = codeName, Name = programName, Description = programDescription, Opcodes = opcodes, ExamplePrivateInputs = examplePrivateInputs, ExamplePublicInputs = examplePublicInputs, ExamplePublicOutputs = examplePublicOutputs, GlobalStepsNoMoreThan = globalStepsNoMoreThan };
    }
}
