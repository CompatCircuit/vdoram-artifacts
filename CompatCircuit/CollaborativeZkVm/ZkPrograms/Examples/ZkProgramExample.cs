using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;

namespace Anonymous.CollaborativeZkVm.ZkPrograms.Examples;
public class ZkProgramExample : ZkProgram {
    public required string CodeName { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<Field> ExamplePublicInputs { get; init; }
    public required IReadOnlyList<Field> ExamplePrivateInputs { get; init; }
    public required IReadOnlyList<Field> ExamplePublicOutputs { get; init; }
    public required int GlobalStepsNoMoreThan { get; init; }

    public List<ZkProgramInstance> GetZkProgramInstances(int partyCount) {
        List<List<Field>> privateInputSharesAllParties = [];
        for (int inputIndex = 0; inputIndex < this.ExamplePrivateInputs.Count; inputIndex++) {
            Field privateInputValue = this.ExamplePrivateInputs[inputIndex];
            List<Field> privateInputShares = ArithConfig.FieldSecretSharing.MakeShares(partyCount, privateInputValue);
            privateInputSharesAllParties.Add(privateInputShares);
        }

        List<ZkProgramInstance> ret = [];
        for (int partyIndex = 0; partyIndex < partyCount; partyIndex++) {
            ZkProgramInstance instance = new() {
                Opcodes = this.Opcodes,
                MyID = partyIndex,
                PartyCount = partyCount,
                PublicInputs = this.ExamplePublicInputs,
                PrivateInputShares = privateInputSharesAllParties.Select(x => x[partyIndex]).ToList(),
                GlobalStepsNoMoreThan = this.GlobalStepsNoMoreThan,
            };
            ret.Add(instance);
        }
        return ret;
    }
}
