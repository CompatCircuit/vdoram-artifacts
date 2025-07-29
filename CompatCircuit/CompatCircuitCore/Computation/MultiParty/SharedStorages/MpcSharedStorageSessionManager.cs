using Anonymous.CompatCircuitCore.Computation.MultiParty.Messages;
using Anonymous.CompatCircuitCore.Computation.MultiParty.Network;
using System.Collections.Concurrent;

namespace Anonymous.CompatCircuitCore.Computation.MultiParty.SharedStorages;
public class MpcSharedStorageSessionManager : IDisposable, IMpcSharedStorageSessionManager {

    private class SessionState {
        public required IMpcSharedStorage? MpcSharedStorage { get; set; }

        /// <summary>
        /// Contains cached messages for each session, until the session ID is registered with an IMpcSharedStorage.
        /// </summary>
        public required List<RawMessage>? Messages { get; set; }
    }
    public IMpcClient MpcClient { get; }

    private ConcurrentDictionary<int, object> SessionLocks { get; } = [];
    private ConcurrentDictionary<int, SessionState> Sessions { get; } = [];

    public MpcSharedStorageSessionManager(IMpcClient mpcClient) {
        ArgumentNullException.ThrowIfNull(mpcClient);

        this.MpcClient = mpcClient;
        this.MpcClient.OnMessageReceived += this.HandleRawMessage;

        Serilog.Log.Debug("MpcSharedStorageSessionManager: initialized");
    }

    private object GetSessionLock(int sessionID) => this.SessionLocks.GetOrAdd(sessionID, _ => new object());

    public void RegisterSession(int sessionID, IMpcSharedStorage sharedStorage) {
        Serilog.Log.Information($"MpcSharedStorageSessionManager: Session {sessionID} is being registered.");

        List<RawMessage>? messages = null;
        SessionState sessionState;
        SessionState newSessionState = new() { MpcSharedStorage = sharedStorage, Messages = null };
        lock (this.GetSessionLock(sessionID)) {
            if (this.Sessions.TryGetValue(sessionID, out SessionState? existingSessionState)) {
                sessionState = existingSessionState;

                if (existingSessionState.MpcSharedStorage is null) {
                    existingSessionState.MpcSharedStorage = sharedStorage;
                    messages = existingSessionState.Messages;
                    existingSessionState.Messages = null;
                }
            }
            else {
                sessionState = newSessionState;
                // Note: although sessionID is locked, the directionary might still be accessed from multiple threads because of different session IDs! That's why we still need a concurrent dictionary.
                this.Sessions[sessionID] = sessionState;
            }
        }

        if (!ReferenceEquals(sessionState.MpcSharedStorage, sharedStorage)) {
            throw new InvalidOperationException($"Session {sessionID} has already registered.");
        }

        if (messages is not null && messages.Count != 0) {
            foreach (RawMessage message in messages) {
                sharedStorage.HandleRawMessage(message);
            }
        }
    }

    public void HandleRawMessage(RawMessage message) {
        ArgumentNullException.ThrowIfNull(message);
        Serilog.Log.Verbose($"MpcSharedStorageSessionManager: received message with session ID {message.SessionID}");

        SessionState sessionState;

        lock (this.GetSessionLock(message.SessionID)) {
            if (this.Sessions.TryGetValue(message.SessionID, out SessionState? existingSessionState)) {
                sessionState = existingSessionState;
                if (sessionState.MpcSharedStorage is null) {
                    sessionState.Messages!.Add(message);
                }
            }
            else {
                sessionState = new SessionState() { Messages = [message], MpcSharedStorage = null };
                // Note: while sessionID is locked, the directionary might still be accessed from multiple threads because of different session ID! That's why we still needs a concurrent dictionary.
                this.Sessions[message.SessionID] = sessionState;
            }
        }

        sessionState.MpcSharedStorage?.HandleRawMessage(message);
    }

    private void HandleRawMessage(object? sender, RawMessage message) => this.HandleRawMessage(message);

    public void UnregisterSession(int sessionID) {
        Serilog.Log.Information($"MpcSharedStorageSessionManager: Session {sessionID} is unregistered.");

        List<RawMessage>? messages = null;

        lock (this.GetSessionLock(sessionID)) {
            if (this.Sessions.TryRemove(sessionID, out SessionState? sessionState)) {
                messages = sessionState?.Messages;
            }
        }

        if (messages is not null && messages.Count != 0) {
            foreach (RawMessage message in messages) {
                this.HandleRawMessage(message);
            }
        }
    }

    public void Dispose() {
        // TODO: whether this class can be successfully disposed? Avoid possible memory leak when a session manager is not needed anymore.

        if (this.MpcClient is not null) {
            this.MpcClient.OnMessageReceived -= this.HandleRawMessage;
        }
    }
    ~MpcSharedStorageSessionManager() {
        this.Dispose();
    }

}
