using System.Diagnostics.CodeAnalysis;

namespace SadPencil.CompatCircuitCore.Computation.MultiParty;
public class MpcExecutorConfig {
    public required int MyID { get; init; }
    public required int PartyCount { get; init; }
    public required int TickMS { get; init; }

    public MpcExecutorConfig() { }

    [SetsRequiredMembers]
    public MpcExecutorConfig(MpcConfig mpcConfig) {
        this.MyID = mpcConfig.MyID;
        this.PartyCount = mpcConfig.PartyCount;
        this.TickMS = mpcConfig.TickMS;
    }
}
