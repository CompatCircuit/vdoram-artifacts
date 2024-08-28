namespace SadPencil.CompatCircuitCore.Computation.MultiParty.SharedStorages;
public interface IMpcSharedStorageFactory {
    public IMpcSharedStorage GetSharedStorage(IMpcSharedStorageSessionManager manager, int sessionID, int partyCount);
}
