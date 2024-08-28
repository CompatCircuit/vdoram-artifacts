using SadPencil.CompatCircuitCore.Computation.MultiParty.Messages;
using SadPencil.CompatCircuitCore.Computation.MultiParty.Network;

namespace SadPencil.CompatCircuitCore.Computation.MultiParty.SharedStorages;
public class DummyMpcSharedStorageSessionManager : IMpcSharedStorageSessionManager {
    public IMpcClient MpcClient { get; }
    public void HandleRawMessage(RawMessage message) { }
    public void RegisterSession(int sessionID, IMpcSharedStorage sharedStorage) { }
    public void UnregisterSession(int sessionID) { }
    public DummyMpcSharedStorageSessionManager() => this.MpcClient = new DummyMpcClient();
    public DummyMpcSharedStorageSessionManager(IMpcClient mpcClient) => this.MpcClient = mpcClient;
}
