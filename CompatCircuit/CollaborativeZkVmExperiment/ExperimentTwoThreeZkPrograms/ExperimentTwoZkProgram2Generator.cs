using SadPencil.CollaborativeZkVm.ZkPrograms;
using SadPencil.CollaborativeZkVm.ZkVmCircuits;

namespace SadPencil.CollaborativeZkVmExperiment.ExperimentTwoThreeZkPrograms;
public class ExperimentTwoZkProgram2Generator : ExperimentTwoThreeZkProgramGeneratorBase {
    protected override string CodeName => "exp2_2";
    protected override ZkProgramOpcode GetNewOp(int step) => NewOp(ZkVmOpType.LessThan, 0, 0, 0);
}
