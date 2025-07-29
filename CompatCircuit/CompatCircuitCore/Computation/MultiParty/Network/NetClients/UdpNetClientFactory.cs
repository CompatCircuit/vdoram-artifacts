namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Network.NetClients;
public class UdpNetClientFactory : INetClientFactory {
    public UdpNetClient NewNetClient() => new();
    INetClient INetClientFactory.NewNetClient() => this.NewNetClient();
}
