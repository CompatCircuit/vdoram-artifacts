using Anonymous.CollaborativeZkVm.ZkPrograms.Examples;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentFourZkPrograms;
public abstract class ExperimentFourZkProgramGeneratorBase : IZkProgramExampleGenerator {
    protected abstract string CodeNameOverride { get; }
    protected abstract ZkProgramExample ZkProgramExample { get; }

    public ZkProgramExample GetZkProgram() {
        ZkProgramExample ret = new() {
            CodeName = this.CodeNameOverride, // Override the original code name
            Name = this.ZkProgramExample.Name,
            Description = this.ZkProgramExample.Description,
            Opcodes = this.ZkProgramExample.Opcodes,
            ExamplePrivateInputs = this.ZkProgramExample.ExamplePrivateInputs,
            ExamplePublicInputs = this.ZkProgramExample.ExamplePublicInputs,
            ExamplePublicOutputs = this.ZkProgramExample.ExamplePublicOutputs,
            GlobalStepsNoMoreThan = this.ZkProgramExample.GlobalStepsNoMoreThan,
        };

        return ret;
    }
}
