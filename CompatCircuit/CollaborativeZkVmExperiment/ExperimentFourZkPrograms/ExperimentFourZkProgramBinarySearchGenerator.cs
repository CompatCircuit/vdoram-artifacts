using Anonymous.CollaborativeZkVm.ZkPrograms.Examples;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentFourZkPrograms;
public class ExperimentFourZkProgramBinarySearchGenerator : ExperimentFourZkProgramGeneratorBase {
    protected override string CodeNameOverride { get; } = "exp4_binary";
    protected override ZkProgramExample ZkProgramExample { get; } = new BinarySearchProgramGenerator().GetZkProgram();
}