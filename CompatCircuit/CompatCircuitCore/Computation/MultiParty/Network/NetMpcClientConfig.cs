using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Network;
public class NetMpcClientConfig {
    public required IReadOnlyList<IPEndPoint> PartyDataEndpoints { get; init; }
    public required IReadOnlyList<IPEndPoint>? PartyAckEndpoints { get; init; }
    public required IPEndPoint MyDataEndpoint { get; init; }
    public required IPEndPoint? MyAckEndpoint { get; init; }
    public required int MyID { get; init; }
    public int DataPort { get; init; } = 12400;
    public int AckPort { get; init; } = 12401;
    public int TimeoutMS { get; init; } = 1000;
    public int MaximumByteCount { get; init; } = 256;

    public NetMpcClientConfig() { }

    [SetsRequiredMembers]
    public NetMpcClientConfig(MpcConfig mpcConfig) {
        this.PartyDataEndpoints = mpcConfig.PartyIPAddresses.Select(address => new IPEndPoint(address, mpcConfig.DataPort)).ToList();
        this.PartyAckEndpoints = null;// mpcConfig.PartyIPAddresses.Select(address => new IPEndPoint(address, mpcConfig.AckPort)).ToList();
        this.MyDataEndpoint = new IPEndPoint(mpcConfig.MyIPAddress, mpcConfig.DataPort);
        this.MyAckEndpoint = null;//new IPEndPoint(mpcConfig.MyIPAddress, mpcConfig.AckPort);
        this.MyID = mpcConfig.MyID;
        this.MaximumByteCount = mpcConfig.MaximumBytesLength;
        this.TimeoutMS = mpcConfig.TimeoutMS;
    }
}
