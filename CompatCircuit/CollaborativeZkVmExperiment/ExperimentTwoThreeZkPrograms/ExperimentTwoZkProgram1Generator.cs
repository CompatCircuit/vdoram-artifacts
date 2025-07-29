using Anonymous.CollaborativeZkVm.ZkPrograms;
using Anonymous.CollaborativeZkVm.ZkVmCircuits;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentTwoThreeZkPrograms;
public class ExperimentTwoZkProgram1Generator : ExperimentTwoThreeZkProgramGeneratorBase {
    protected override string CodeName => "exp2_1";
    protected override ZkProgramOpcode GetNewOp(int step) => NewOp(ZkVmOpType.Mul, 0, 0, 0);
}
