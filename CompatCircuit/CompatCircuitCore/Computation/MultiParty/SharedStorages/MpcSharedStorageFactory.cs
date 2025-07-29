namespace Anonymous.CompatCircuitCore.Computation.MultiParty.SharedStorages;
public class MpcSharedStorageFactory : IMpcSharedStorageFactory {
    public MpcSharedStorage GetSharedStorage(IMpcSharedStorageSessionManager manager, int sessionID, int partyCount) => new(manager, sessionID, partyCount);
    IMpcSharedStorage IMpcSharedStorageFactory.GetSharedStorage(IMpcSharedStorageSessionManager manager, int sessionID, int partyCount) => this.GetSharedStorage(manager, sessionID, partyCount);
}
