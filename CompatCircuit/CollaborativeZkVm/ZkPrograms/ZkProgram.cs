namespace Anonymous.CollaborativeZkVm.ZkPrograms;
public class ZkProgram {
    public required IReadOnlyList<ZkProgramOpcode> Opcodes { get; init; }

    static ZkProgram() => ZkProgramOpcodeJsonConverter.Initialize();
}
