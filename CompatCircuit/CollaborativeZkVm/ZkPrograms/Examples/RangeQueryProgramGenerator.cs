using Anonymous.CollaborativeZkVm.ZkVmCircuits;
using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;
using System.Numerics;

namespace Anonymous.CollaborativeZkVm.ZkPrograms.Examples;
public class RangeQueryProgramGenerator : IZkProgramExampleGenerator {
    public ZkProgramExample GetZkProgram() {
        static Field NewField(BigInteger val) => ArithConfig.FieldFactory.New(val);
        static ZkProgramOpcode NewOp(ZkVmOpType opType, int? arg0 = null, int? arg1 = null, int? arg2 = null) => new(opType, NewField(arg0 ?? 0), NewField(arg1 ?? 0), NewField(arg2 ?? 0));
        string codeName = "RangeQuery";
        string programName = "Range Query";
        string programDescription = "Filters a list of individuals based on age criteria. Takes the number of individuals as public input, then for each individual, takes their ID and age as private inputs. The program outputs the IDs of individuals whose age is between 20 and 50 (exclusive).";
        List<ZkProgramOpcode> opcodes = [
            NewOp(ZkVmOpType.Set, 8, 0),
            NewOp(ZkVmOpType.Set, 9, 2),
            NewOp(ZkVmOpType.Set, 4, 20),
            NewOp(ZkVmOpType.Set, 5, 50),
            NewOp(ZkVmOpType.PublicInput, 1),
            NewOp(ZkVmOpType.Mul, 1, 1, 9),
            NewOp(ZkVmOpType.Set, 0, 0),
            NewOp(ZkVmOpType.LessThan, 6, 0, 1),
            NewOp(ZkVmOpType.JumpIfZero, 6, 18),
            NewOp(ZkVmOpType.PrivateInput, 2),
            NewOp(ZkVmOpType.PrivateInput, 3),
            NewOp(ZkVmOpType.LessThan, 6, 4, 3),
            NewOp(ZkVmOpType.JumpIfZero, 6, 14),
            NewOp(ZkVmOpType.LessThan, 7, 3, 5),
            NewOp(ZkVmOpType.JumpIfZero, 7, 16),
            NewOp(ZkVmOpType.PublicOutput, 2),
            NewOp(ZkVmOpType.Add, 0, 0, 9),
            NewOp(ZkVmOpType.JumpIfZero, 8, 7),
            NewOp(ZkVmOpType.Halt),
        ];

        List<Field> examplePublicInputs = new List<int> { 10 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePrivateInputs = new List<int> { 1001, 18,
    1002, 25,
    1003, 52,
    1004, 32,
    1005, 50,
    1006, 19,
    1007, 45,
    1008, 20,
    1009, 49,
    1010, 55 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        List<Field> examplePublicOutputs = new List<int> { 1002, 1004, 1007, 1008, 1009 }.Select(x => ArithConfig.FieldFactory.New(x)).ToList();
        int globalStepsNoMoreThan = opcodes.Count * examplePrivateInputs.Count * examplePrivateInputs.Count;

        return new ZkProgramExample() { CodeName = codeName, Name = programName, Description = programDescription, Opcodes = opcodes, ExamplePrivateInputs = examplePrivateInputs, ExamplePublicInputs = examplePublicInputs, ExamplePublicOutputs = examplePublicOutputs, GlobalStepsNoMoreThan = globalStepsNoMoreThan };
    }
}
