using Anonymous.CollaborativeZkVm.ZkPrograms;
using Anonymous.CollaborativeZkVm.ZkPrograms.Examples;
using Anonymous.CollaborativeZkVm.ZkVmCircuits;
using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;
using System.Numerics;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentTwoThreeZkPrograms;
public abstract class ExperimentTwoThreeZkProgramGeneratorBase : IZkProgramExampleGenerator {
    public int ProgramStepCount { get; init; } = 5;
    protected abstract string CodeName { get; }
    protected static Field NewField(BigInteger val) => ArithConfig.FieldFactory.New(val);
    protected static ZkProgramOpcode NewOp(ZkVmOpType opType, int? arg0 = null, int? arg1 = null, int? arg2 = null) => new(opType, NewField(arg0 ?? 0), NewField(arg1 ?? 0), NewField(arg2 ?? 0));
    protected abstract ZkProgramOpcode GetNewOp(int step);
    public ZkProgramExample GetZkProgram() {
        string codeName = this.CodeName;
        string programName = this.CodeName;
        string programDescription = this.CodeName;
        List<ZkProgramOpcode> opcodes = Enumerable.Range(0, this.ProgramStepCount - 1).Select(this.GetNewOp).Concat([NewOp(ZkVmOpType.Halt, 0, 0, 0)]).ToList();
        List<Field> examplePublicInputs = [];
        List<Field> examplePrivateInputs = [];
        List<Field> examplePublicOutputs = [];
        int globalStepsNoMoreThan = opcodes.Count;

        return new ZkProgramExample() { CodeName = codeName, Name = programName, Description = programDescription, Opcodes = opcodes, ExamplePrivateInputs = examplePrivateInputs, ExamplePublicInputs = examplePublicInputs, ExamplePublicOutputs = examplePublicOutputs, GlobalStepsNoMoreThan = globalStepsNoMoreThan };
    }
}
