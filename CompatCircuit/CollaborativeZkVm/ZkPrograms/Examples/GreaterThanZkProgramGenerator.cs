using Anonymous.CollaborativeZkVm.ZkVmCircuits;
using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;
using System.Numerics;

namespace Anonymous.CollaborativeZkVm.ZkPrograms.Examples;
public class GreaterThanZkProgramGenerator : IZkProgramExampleGenerator {
    public ZkProgramExample GetZkProgram() {
        static Field NewField(BigInteger val) => ArithConfig.FieldFactory.New(val);
        static ZkProgramOpcode NewOp(ZkVmOpType opType, int? arg0 = null, int? arg1 = null, int? arg2 = null) => new(opType, NewField(arg0 ?? 0), NewField(arg1 ?? 0), NewField(arg2 ?? 0));
        string codeName = "NumberComparison";
        string programName = "Number Comparison";
        string programDescription = "Compares two numbers and outputs 1 if the first number is greater than the second number, otherwise outputs 0.";
        List<ZkProgramOpcode> opcodes = [
            NewOp(ZkVmOpType.PrivateInput, 0),
            NewOp(ZkVmOpType.Move, 1, 0),
            NewOp(ZkVmOpType.PrivateInput, 0),
            NewOp(ZkVmOpType.LessThan, 0, 0, 1),
            NewOp(ZkVmOpType.PublicOutput, 0),
            NewOp(ZkVmOpType.Halt),
        ];

        List<Field> examplePublicInputs = [];
        List<Field> examplePrivateInputs = new List<int> { 1919810, 114514 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePublicOutputs = new List<int> { 1 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        int globalStepsNoMoreThan = opcodes.Count;

        return new ZkProgramExample() { CodeName = codeName, Name = programName, Description = programDescription, Opcodes = opcodes, ExamplePrivateInputs = examplePrivateInputs, ExamplePublicInputs = examplePublicInputs, ExamplePublicOutputs = examplePublicOutputs, GlobalStepsNoMoreThan = globalStepsNoMoreThan };
    }
}
