using Anonymous.CollaborativeZkVm.ZkVmCircuits;
using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;
using System.Numerics;

namespace Anonymous.CollaborativeZkVm.ZkPrograms.Examples;
public class ReverseInputProgramGenerator : IZkProgramExampleGenerator {
    public ZkProgramExample GetZkProgram() {
        static Field NewField(BigInteger val) => ArithConfig.FieldFactory.New(val);
        static ZkProgramOpcode NewOp(ZkVmOpType opType, int? arg0 = null, int? arg1 = null, int? arg2 = null) => new(opType, NewField(arg0 ?? 0), NewField(arg1 ?? 0), NewField(arg2 ?? 0));
        string codeName = "ReverseInput";
        string programName = "Reverse Input";
        string programDescription = "Input the array length n as public, then input n values as private. The program outputs n values in reserved order.";

        List<ZkProgramOpcode> opcodes = [
            // Input
            NewOp(ZkVmOpType.Set, 15, 1), // Line 0: R15 = 1
            NewOp(ZkVmOpType.PublicInput, 1), // Line 1: R1 = array length
            NewOp(ZkVmOpType.Set, 0, 0), // Line 2: R0 = 0
            NewOp(ZkVmOpType.PrivateInput, 2), // Line 3: R2 = array[R0]
            NewOp(ZkVmOpType.Store, 2), // Line 4: memory[R0] = R2
            NewOp(ZkVmOpType.Add, 0, 0, 15), // Line 5: R0 = R0 + 1
            NewOp(ZkVmOpType.LessThan, 2, 0, 1), // Line 6: R2 = bool(R0 < R1)
            NewOp(ZkVmOpType.Not, 2, 2), // Line 7: R2 = NOT R2
            NewOp(ZkVmOpType.JumpIfZero, 2, 3), // Line 8: if R0 < R1, goto Line 3

            // Output
            NewOp(ZkVmOpType.Sub, 4, 1, 15), // Line 9: R4 = R1 - 1
            NewOp(ZkVmOpType.Set, 2, 0), // Line 10: R2 = 0
            NewOp(ZkVmOpType.Sub, 0, 4, 2), // Line 11: R0 = R4 - R2
            NewOp(ZkVmOpType.Load, 3), // Line 12: R3 = memory[R0]
            NewOp(ZkVmOpType.PublicOutput, 3), // Line 13: output array[R0]
            NewOp(ZkVmOpType.Add, 2, 2, 15), // Line 14: R2 = R2 + 1
            NewOp(ZkVmOpType.LessThan, 3, 2, 1), // Line 15: R3 = bool(R2 < R1)
            NewOp(ZkVmOpType.Not, 3, 3), // Line 16: R3 = NOT R3
            NewOp(ZkVmOpType.JumpIfZero, 3, 11), // Line 17: if R0 < R1, goto Line 11
            NewOp(ZkVmOpType.Halt),
        ];

        List<Field> examplePublicInputs = new List<int> { 4 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePrivateInputs = new List<int> { 1, 2, 3, 4 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePublicOutputs = new List<int> { 4, 3, 2, 1 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        int globalStepsNoMoreThan = opcodes.Count * examplePrivateInputs.Count;

        return new ZkProgramExample() { CodeName = codeName, Name = programName, Description = programDescription, Opcodes = opcodes, ExamplePrivateInputs = examplePrivateInputs, ExamplePublicInputs = examplePublicInputs, ExamplePublicOutputs = examplePublicOutputs, GlobalStepsNoMoreThan = globalStepsNoMoreThan };
    }
}
