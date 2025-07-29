using Anonymous.CollaborativeZkVm.ZkPrograms.Examples;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentFourZkPrograms;
public class ExperimentFourZkProgramSlidingWindowGenerator : ExperimentFourZkProgramGeneratorBase {
    protected override string CodeNameOverride { get; } = "exp4_slide";
    protected override ZkProgramExample ZkProgramExample { get; } = new SlidingWindowProgramGenerator().GetZkProgram();
}