using Anonymous.CollaborativeZkVm.ZkVmCircuits;
using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;
using System.Numerics;
namespace Anonymous.CollaborativeZkVm.ZkPrograms.Examples;
public class EuclideanProgramGenerator : IZkProgramExampleGenerator {
    public ZkProgramExample GetZkProgram() {
        static Field NewField(BigInteger val) => ArithConfig.FieldFactory.New(val);
        static ZkProgramOpcode NewOp(ZkVmOpType opType, int? arg0 = null, int? arg1 = null, int? arg2 = null) => new(opType, NewField(arg0 ?? 0), NewField(arg1 ?? 0), NewField(arg2 ?? 0));
        string codeName = "EuclideanAlgorithm";
        string programName = "Euclidean Algorithm";
        string programDescription = "Calculates the greatest common divisor (GCD) of two integers using the Euclidean algorithm. Takes two private integers as input and outputs the GCD.";
        List<ZkProgramOpcode> opcodes = [
            NewOp(ZkVmOpType.PrivateInput, 0),
            NewOp(ZkVmOpType.PrivateInput, 1),
            NewOp(ZkVmOpType.Norm, 5, 1),
            NewOp(ZkVmOpType.JumpIfZero, 5, 9),
            NewOp(ZkVmOpType.LessThan, 5, 0, 1),
            NewOp(ZkVmOpType.JumpIfZero, 5, 7),
            NewOp(ZkVmOpType.Swap, 0, 1),
            NewOp(ZkVmOpType.Sub, 0, 0, 1),
            NewOp(ZkVmOpType.JumpIfZero, 2, 2),
            NewOp(ZkVmOpType.PublicOutput, 0),
            NewOp(ZkVmOpType.Halt),
        ];

        List<Field> examplePublicInputs = [];
        List<Field> examplePrivateInputs = new List<int> { 48, 18 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePublicOutputs = new List<int> { 6 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        int globalStepsNoMoreThan = opcodes.Count;
        return new ZkProgramExample() { CodeName = codeName, Name = programName, Description = programDescription, Opcodes = opcodes, ExamplePrivateInputs = examplePrivateInputs, ExamplePublicInputs = examplePublicInputs, ExamplePublicOutputs = examplePublicOutputs, GlobalStepsNoMoreThan = globalStepsNoMoreThan };
    }
}
