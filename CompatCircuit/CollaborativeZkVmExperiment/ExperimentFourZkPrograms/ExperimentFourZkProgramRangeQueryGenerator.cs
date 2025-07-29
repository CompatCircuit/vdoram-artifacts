using Anonymous.CollaborativeZkVm.ZkPrograms.Examples;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentFourZkPrograms;
public class ExperimentFourZkProgramRangeQueryGenerator : ExperimentFourZkProgramGeneratorBase {
    protected override string CodeNameOverride { get; } = "exp4_range";
    protected override ZkProgramExample ZkProgramExample { get; } = new RangeQueryProgramGenerator().GetZkProgram();

}