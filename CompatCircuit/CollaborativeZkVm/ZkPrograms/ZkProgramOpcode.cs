using SadPencil.CollaborativeZkVm.ZkVmCircuits;
using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.GlobalConfig;
using System.Diagnostics.CodeAnalysis;

namespace SadPencil.CollaborativeZkVm.ZkPrograms;
public record ZkProgramOpcode {
    public required ZkVmOpType OpType { get; init; }
    public required Field Arg0 { get; init; }
    public required Field Arg1 { get; init; }
    public required Field Arg2 { get; init; }

    public ZkProgramOpcode() { }

    [SetsRequiredMembers]
    public ZkProgramOpcode(ZkVmOpType opType, Field? arg0 = null, Field? arg1 = null, Field? arg2 = null) {
        Field zero = ArithConfig.FieldFactory.Zero;
        this.OpType = opType;
        this.Arg0 = arg0 ?? zero;
        this.Arg1 = arg1 ?? zero;
        this.Arg2 = arg2 ?? zero;
    }

    public sealed override string ToString() => $"{(byte)this.OpType} {this.Arg0} {this.Arg1} {this.Arg2}";

    public static ZkProgramOpcode FromString(string str) {
        string[] parts = str.Split(' ');
        return parts.Length != 4
            ? throw new FormatException("Invalid ZkProgramOpcode format")
            : new ZkProgramOpcode(
            (ZkVmOpType)byte.Parse(parts[0]),
            ArithConfig.FieldFactory.New(int.Parse(parts[1])),
            ArithConfig.FieldFactory.New(int.Parse(parts[2])),
            ArithConfig.FieldFactory.New(int.Parse(parts[3]))
        );
    }

    static ZkProgramOpcode() => ZkProgramOpcodeJsonConverter.Initialize();
}
