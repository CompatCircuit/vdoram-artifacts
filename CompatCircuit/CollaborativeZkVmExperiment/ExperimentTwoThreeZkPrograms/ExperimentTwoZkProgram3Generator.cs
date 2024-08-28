using SadPencil.CollaborativeZkVm.ZkPrograms;
using SadPencil.CollaborativeZkVm.ZkVmCircuits;

namespace SadPencil.CollaborativeZkVmExperiment.ExperimentTwoThreeZkPrograms;
public class ExperimentTwoZkProgram3Generator : ExperimentTwoThreeZkProgramGeneratorBase {
    protected override string CodeName => "exp2_3";
    protected override ZkProgramOpcode GetNewOp(int step) => NewOp(ZkVmOpType.Hash, 0, 0, 0);
}
