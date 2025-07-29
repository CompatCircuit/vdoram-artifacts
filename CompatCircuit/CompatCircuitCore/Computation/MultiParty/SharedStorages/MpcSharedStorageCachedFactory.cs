namespace Anonymous.CompatCircuitCore.Computation.MultiParty.SharedStorages;
/// <summary>
/// This factory caches and returns the same MpcSharedStorage object for an existing session ID.
/// Made primarily for unit tests where a MpcSharedStorage needs to be shared among multiple MPC executors.
/// Please use DummyMpcSharedStorageSessionManager in this case.
/// </summary>
public class MpcSharedStorageCachedFactory : IMpcSharedStorageFactory {
    public MpcSharedStorageCachedFactory() { }
    public MpcSharedStorageFactory MpcSharedStorageFactory { get; init; } = new();

    /// <summary>
    /// Automatically remove cached MpcSharedStorage upon reaching this specified retrieval count, for reduing RAM usage.
    /// Usually, specifying it as the MPC party count simultaneously running in this process.
    /// </summary>
    public required int ExpireAfterRetrievalCount { get; init; }

    private Dictionary<int, MpcSharedStorage> SharedStorages { get; } = [];
    private Dictionary<int, int> SharedStoragesCounts { get; } = [];
    private HashSet<int> ExpiredSharedStorages { get; } = [];

    private readonly object _lock = new();
    public MpcSharedStorage GetSharedStorage(IMpcSharedStorageSessionManager manager, int sessionID, int partyCount) {
        lock (this._lock) {
            if (this.ExpiredSharedStorages.Contains(sessionID)) {
                throw new Exception($"S ession ID {sessionID} has reached its expire limitation.");
            }

            MpcSharedStorage ret;
            if (this.SharedStorages.TryGetValue(sessionID, out MpcSharedStorage? value)) {
                Serilog.Log.Debug($"MpcSharedStorageCachedFactory: fetch existing MpcSharedStorage for Session {sessionID}");
                ret = value;
            }
            else {
                Serilog.Log.Debug($"MpcSharedStorageCachedFactory: new MpcSharedStorage for Session {sessionID}");
                ret = this.MpcSharedStorageFactory.GetSharedStorage(manager, sessionID, partyCount);
                this.SharedStorages.Add(sessionID, ret);
            }

            int oldCount = this.SharedStoragesCounts.GetValueOrDefault(sessionID, 0);
            int newCount = oldCount + 1;
            this.SharedStoragesCounts[sessionID] = newCount;

            if (this.ExpireAfterRetrievalCount != -1 && newCount >= this.ExpireAfterRetrievalCount) {
                Serilog.Log.Debug($"MpcSharedStorageCachedFactory: expired MpcSharedStorage for Session {sessionID}");
                _ = this.SharedStorages.Remove(sessionID);
                _ = this.ExpiredSharedStorages.Add(sessionID);
            }
            return ret;
        }
    }

    IMpcSharedStorage IMpcSharedStorageFactory.GetSharedStorage(IMpcSharedStorageSessionManager manager, int sessionID, int partyCount) => this.GetSharedStorage(manager, sessionID, partyCount);
}
