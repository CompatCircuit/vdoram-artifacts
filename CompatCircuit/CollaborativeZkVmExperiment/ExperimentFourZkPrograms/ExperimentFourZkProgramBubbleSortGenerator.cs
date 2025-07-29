using Anonymous.CollaborativeZkVm.ZkPrograms.Examples;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentFourZkPrograms;
public class ExperimentFourZkProgramBubbleSortGenerator : ExperimentFourZkProgramGeneratorBase {
    protected override string CodeNameOverride { get; } = "exp4_bubble";
    protected override ZkProgramExample ZkProgramExample { get; } = new BubbleSortProgramGenerator().GetZkProgram();
}