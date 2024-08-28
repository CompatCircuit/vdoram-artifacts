using System.Net;
using System.Text.Json.Serialization;

namespace SadPencil.CompatCircuitCore.Computation.MultiParty;
public class MpcConfig {
    public required IReadOnlyList<IPAddress> PartyIPAddresses { get; init; }
    public required IPAddress MyIPAddress { get; init; }
    public required int MyID { get; init; }
    public int DataPort { get; init; } = 12400;
    //public int AckPort { get; init; } = 0;
    public int MaximumBytesLength { get; init; } = 256;

    [JsonIgnore]
    public int PartyCount => this.PartyIPAddresses.Count;

    public int TickMS { get; init; } = 10;
    public int TimeoutMS { get; init; } = 1000;
}
