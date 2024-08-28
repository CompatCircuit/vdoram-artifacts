namespace SadPencil.CompatCircuitCore.Computation.MultiParty.Network.NetClients;
public class TcpNetClientFactory : INetClientFactory {
    public TcpNetClient NewNetClient() => new();
    INetClient INetClientFactory.NewNetClient() => this.NewNetClient();
}
