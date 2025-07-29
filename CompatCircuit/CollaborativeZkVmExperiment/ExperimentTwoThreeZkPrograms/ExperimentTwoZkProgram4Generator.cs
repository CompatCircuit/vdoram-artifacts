using Anonymous.CollaborativeZkVm.ZkPrograms;
using Anonymous.CollaborativeZkVm.ZkVmCircuits;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentTwoThreeZkPrograms;
public class ExperimentTwoZkProgram4Generator : ExperimentTwoThreeZkProgramGeneratorBase {
    protected override string CodeName => "exp2_4";
    protected override ZkProgramOpcode GetNewOp(int step) => NewOp(ZkVmOpType.Store, 0, 0, 0);
}
