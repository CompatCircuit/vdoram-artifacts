using Anonymous.CollaborativeZkVm.ZkPrograms.Examples;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentFourZkPrograms;
public class ExperimentFourZkProgramFibonacciGenerator : ExperimentFourZkProgramGeneratorBase {
    protected override string CodeNameOverride { get; } = "exp4_fib";
    protected override ZkProgramExample ZkProgramExample { get; } = new FibonacciProgramGenerator().GetZkProgram();
}
