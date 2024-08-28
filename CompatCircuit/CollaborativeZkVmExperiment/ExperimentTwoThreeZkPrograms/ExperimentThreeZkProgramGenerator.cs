using SadPencil.CollaborativeZkVm.ZkPrograms;
using SadPencil.CollaborativeZkVm.ZkVmCircuits;

namespace SadPencil.CollaborativeZkVmExperiment.ExperimentTwoThreeZkPrograms;
public class ExperimentThreeZkProgramGenerator : ExperimentTwoThreeZkProgramGeneratorBase {
    protected override string CodeName { get; }

    public ExperimentThreeZkProgramGenerator(string codeName) => this.CodeName = codeName;

    protected override ZkProgramOpcode GetNewOp(int step) => NewOp(ZkVmOpType.Mul, 0, 0, 0);
}
