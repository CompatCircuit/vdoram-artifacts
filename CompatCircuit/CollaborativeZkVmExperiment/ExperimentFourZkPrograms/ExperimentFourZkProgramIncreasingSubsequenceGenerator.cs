using Anonymous.CollaborativeZkVm.ZkPrograms.Examples;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentFourZkPrograms;
public class ExperimentFourZkProgramIncreasingSubsequenceGenerator : ExperimentFourZkProgramGeneratorBase {
    protected override string CodeNameOverride { get; } = "exp4_increasing";
    protected override ZkProgramExample ZkProgramExample { get; } = new LongestContinuousIncreasingSubsequenceProgramGenerator().GetZkProgram();
}