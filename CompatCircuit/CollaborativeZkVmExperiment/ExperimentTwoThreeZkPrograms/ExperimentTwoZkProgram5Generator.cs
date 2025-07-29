using Anonymous.CollaborativeZkVm.ZkPrograms;
using Anonymous.CollaborativeZkVm.ZkVmCircuits;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentTwoThreeZkPrograms;
public class ExperimentTwoZkProgram5Generator : ExperimentTwoThreeZkProgramGeneratorBase {
    protected override string CodeName => "exp2_5";
    protected override ZkProgramOpcode GetNewOp(int step) => NewOp(ZkVmOpType.Load, 0, 0, 0);
}
