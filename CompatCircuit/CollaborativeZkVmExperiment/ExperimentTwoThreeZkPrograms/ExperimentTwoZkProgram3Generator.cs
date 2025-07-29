using Anonymous.CollaborativeZkVm.ZkPrograms;
using Anonymous.CollaborativeZkVm.ZkVmCircuits;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentTwoThreeZkPrograms;
public class ExperimentTwoZkProgram3Generator : ExperimentTwoThreeZkProgramGeneratorBase {
    protected override string CodeName => "exp2_3";
    protected override ZkProgramOpcode GetNewOp(int step) => NewOp(ZkVmOpType.Hash, 0, 0, 0);
}
