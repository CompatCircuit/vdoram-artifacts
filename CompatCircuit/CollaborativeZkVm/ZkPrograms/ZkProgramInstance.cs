using Anonymous.CompatCircuitCore.Arithmetic;

namespace Anonymous.CollaborativeZkVm.ZkPrograms;
public class ZkProgramInstance : ZkProgram {
    public required int MyID { get; init; }
    public required int PartyCount { get; init; }
    public required IReadOnlyList<Field> PublicInputs { get; init; }
    public required IReadOnlyList<Field> PrivateInputShares { get; init; }

    public required int GlobalStepsNoMoreThan { get; init; }

    static ZkProgramInstance() => ZkProgramOpcodeJsonConverter.Initialize();
}
