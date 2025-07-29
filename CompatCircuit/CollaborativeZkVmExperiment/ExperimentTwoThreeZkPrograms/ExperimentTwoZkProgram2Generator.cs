using Anonymous.CollaborativeZkVm.ZkPrograms;
using Anonymous.CollaborativeZkVm.ZkVmCircuits;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentTwoThreeZkPrograms;
public class ExperimentTwoZkProgram2Generator : ExperimentTwoThreeZkProgramGeneratorBase {
    protected override string CodeName => "exp2_2";
    protected override ZkProgramOpcode GetNewOp(int step) => NewOp(ZkVmOpType.LessThan, 0, 0, 0);
}
