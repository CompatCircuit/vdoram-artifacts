using SadPencil.CompatCircuitCore.Computation.MultiParty.Messages;
using SadPencil.CompatCircuitCore.Computation.MultiParty.Network;
using SadPencil.CompatCircuitCore.Extensions;
using System.Collections;
using System.Collections.Concurrent;
using System.Numerics;

namespace SadPencil.CompatCircuitCore.Computation.MultiParty.SharedStorages;
public class MpcSharedStorage : IMpcSharedStorage, IDisposable {
    public int SessionID { get; }
    public int PartyCount { get; }
    private IMpcClient MpcClient => this.MpcSharedStorageSessionManager.MpcClient;

    private IMpcSharedStorageSessionManager MpcSharedStorageSessionManager { get; }

    private bool Unregistered { get; set; } = false;

    public MpcSharedStorage(IMpcSharedStorageSessionManager manager, int sessionID, int partyCount) {
        this.MpcSharedStorageSessionManager = manager;
        this.SessionID = sessionID;
        this.PartyCount = partyCount;

        Serilog.Log.Information($"MpcSharedStorage: Session {this.SessionID} is created.");

        this.MpcSharedStorageSessionManager.RegisterSession(sessionID, this);

    }

    ~MpcSharedStorage() {
        Serilog.Log.Verbose($"MpcSharedStorage: Session {this.SessionID} is being finalized.");
        this.Dispose();
    }

    public void UnregisterSession() {
        if (this.Unregistered) {
            return;
        }
        Serilog.Log.Information($"MpcSharedStorage: Unregister session {this.SessionID}.");
        this.Unregistered = true;
        this.MpcSharedStorageSessionManager.UnregisterSession(this.SessionID);
    }

    public void Dispose() {
        Serilog.Log.Information($"MpcSharedStorage: Session {this.SessionID} is being disposed.");
        this.UnregisterSession();
        this._partyCompleted = null;
        this._partyOnline = null;
        this._exposedBoolShareVectors = null;
        this._exposedBigIntegerShareVectors = null;
        GC.SuppressFinalize(this);
    }

    private bool Started { get; set; } = false;

    private void BroadcastMessage<T>(T messagePayload) where T : IMessagePayload<T> {
        RawMessage msg = RawMessageHelper.ComposeRawMessage(this.SessionID, messagePayload);
        this.MpcClient.BroadcastMessage(msg);
    }

    private void SendMessage<T>(int partyIndex, T messagePayload) where T : IMessagePayload<T> {
        RawMessage msg = RawMessageHelper.ComposeRawMessage(this.SessionID, messagePayload);
        this.MpcClient.SendMessage(partyIndex, msg);
    }

    private ConcurrentDictionary<int, bool>? _partyOnline = [];
    private ConcurrentDictionary<int, bool> PartyOnline => this._partyOnline!;
    public bool GetPartyOnline(int partyIndex) => this.PartyOnline.GetValueOrDefault(partyIndex);
    public IReadOnlyList<bool> GetPartyOnlineAllParties() => Enumerable.Range(0, this.PartyCount).Select(this.GetPartyOnline).ToList();
    public void SetPartyOnline(int senderPartyIndex) {
        this.PartyOnline[senderPartyIndex] = true;

        // Send message
        Serilog.Log.Verbose($"MpcSharedStorage: Party {senderPartyIndex} sends online message to all parties.");
        this.BroadcastMessage(new PartyOnlineMessagePayload(senderPartyIndex));
    }

    private ConcurrentDictionary<int, bool>? _partyCompleted = [];
    private ConcurrentDictionary<int, bool> PartyCompleted => this._partyCompleted!;
    public bool GetPartyCompleted(int partyIndex) => this.PartyCompleted.GetValueOrDefault(partyIndex);
    public IReadOnlyList<bool> GetPartyCompletedAllParties() => Enumerable.Range(0, this.PartyCount).Select(this.GetPartyCompleted).ToList();
    public void SetPartyCompleted(int senderPartyIndex) {
        this.PartyCompleted[senderPartyIndex] = true;

        // Send message
        Serilog.Log.Verbose($"MpcSharedStorage: Party {senderPartyIndex} sends completion message to all parties.");
        this.BroadcastMessage(new PartyCompletedMessagePayload(senderPartyIndex));
    }

    private ConcurrentDictionary<string, List<IReadOnlyList<bool>?>>? _exposedBoolShareVectors = [];
    private ConcurrentDictionary<string, List<IReadOnlyList<bool>?>> ExposedBoolShareVectors => this._exposedBoolShareVectors!;
    private List<IReadOnlyList<bool>?> NewExposedBoolShareVectorAllParties() => Enumerable.Repeat<IReadOnlyList<bool>?>(null, this.PartyCount).ToList();
    public IReadOnlyList<IReadOnlyList<bool>?>? GetExposedBoolShareVectorAllParties(string key) => this.ExposedBoolShareVectors.GetValueOrDefault(key);
    public void SetExposedBoolShareVector(string key, int senderPartyIndex, IReadOnlyList<bool> shares) {
        List<IReadOnlyList<bool>?> sharesAllParties = this.ExposedBoolShareVectors.GetOrAdd(key, _ => this.NewExposedBoolShareVectorAllParties());
        lock (sharesAllParties) {
            sharesAllParties[senderPartyIndex] = shares;
        }

        // Send message
        Serilog.Log.Verbose($"MpcSharedStorage: Party {senderPartyIndex} sent Bool shares to all parties.");
        BitArray bits = new(shares.ToArray());
        this.BroadcastMessage(new BoolExposureMessagePayload(
            shareOwnerID: senderPartyIndex,
            exposureKey: key,
            bits: bits));
    }

    private ConcurrentDictionary<string, List<IReadOnlyList<BigInteger>?>>? _exposedBigIntegerShareVectors = [];
    private ConcurrentDictionary<string, List<IReadOnlyList<BigInteger>?>> ExposedBigIntegerShareVectors => this._exposedBigIntegerShareVectors!;
    private List<IReadOnlyList<BigInteger>?> NewExposedBigIntegerShareVectorAllParties() => Enumerable.Repeat<IReadOnlyList<BigInteger>?>(null, this.PartyCount).ToList();
    public IReadOnlyList<IReadOnlyList<BigInteger>?>? GetExposedBigIntegerShareVectorAllParties(string key) => this.ExposedBigIntegerShareVectors.GetValueOrDefault(key);
    public void SetExposedBigIntegerShareVector(string key, int senderPartyIndex, IReadOnlyList<BigInteger> shares) {
        List<IReadOnlyList<BigInteger>?> sharesAllParties = this.ExposedBigIntegerShareVectors.GetOrAdd(key, _ => this.NewExposedBigIntegerShareVectorAllParties());
        lock (sharesAllParties) {
            sharesAllParties[senderPartyIndex] = shares;
        }

        // Send message
        Serilog.Log.Verbose($"MpcSharedStorage: Party {senderPartyIndex} exposed BigInteger shares.");
        this.BroadcastMessage(new BigIntegerExposureMessagePayload(
            shareOwnerID: senderPartyIndex,
            exposureKey: key,
            values: shares));
    }

    private ConcurrentDictionary<string, List<IReadOnlyList<BigInteger>?>> ExposedInputShareVectors { get; } = [];
    private List<IReadOnlyList<BigInteger>?> NewExposedInputShareVectorAllParties() => Enumerable.Repeat<IReadOnlyList<BigInteger>?>(null, this.PartyCount).ToList();
    public IReadOnlyList<BigInteger>? GetInputShareVector(string key, int senderPartyIndex) {
        List<IReadOnlyList<BigInteger>?>? sharesAllParties = this.ExposedInputShareVectors.GetValueOrDefault(key);
        return sharesAllParties?[senderPartyIndex];
    }
    public void SetInputShareVector(string key, int senderPartyIndex, int receiverPartyIndex, IReadOnlyList<BigInteger> shares) {
        List<IReadOnlyList<BigInteger>?> sharesAllParties = this.ExposedInputShareVectors.GetOrAdd(key, _ => this.NewExposedBigIntegerShareVectorAllParties());
        lock (sharesAllParties) {
            sharesAllParties[receiverPartyIndex] = shares;
        }

        // Send message
        Serilog.Log.Verbose($"MpcSharedStorage: Party {senderPartyIndex} sent BigInteger input shares to party {receiverPartyIndex}.");
        this.SendMessage(receiverPartyIndex, new BigIntegerInputShareMessagePayload(
            shareOwnerID: receiverPartyIndex,
            exposureKey: key,
            values: shares));
    }

    public void HandleRawMessage(RawMessage msg) {
        ArgumentNullException.ThrowIfNull(msg);

        Serilog.Log.Verbose($"MpcSharedStorage: Received message with session ID {msg.SessionID}.");
        if (msg.SessionID != this.SessionID) {
            Serilog.Log.Warning($"MpcSharedStorage: Received message with incorrect session ID {msg.SessionID}.");
            return;
        }

        switch (msg.MessagePayloadTypeEnum) {
            case MessagePayloadType.PartyOnline: {
                    PartyOnlineMessagePayload msgPayload = msg.ExtractMessagePayload<PartyOnlineMessagePayload>();
                    this.PartyOnline[msgPayload.PartyID] = true;
                    Serilog.Log.Verbose($"MpcSharedStorage: Party {msgPayload.PartyID} is online.");
                }
                break;
            case MessagePayloadType.PartyCompleted: {
                    PartyCompletedMessagePayload msgPayload = msg.ExtractMessagePayload<PartyCompletedMessagePayload>();
                    this.PartyCompleted[msgPayload.PartyID] = true;
                    Serilog.Log.Verbose($"MpcSharedStorage: Party {msgPayload.PartyID} has completed.");
                }
                break;
            case MessagePayloadType.BigIntegerExposure: {
                    BigIntegerExposureMessagePayload msgPayload = msg.ExtractMessagePayload<BigIntegerExposureMessagePayload>();
                    List<IReadOnlyList<BigInteger>?> sharesAllParties = this.ExposedBigIntegerShareVectors.GetOrAdd(msgPayload.ExposureKey, _ => this.NewExposedBigIntegerShareVectorAllParties());
                    lock (sharesAllParties) {
                        sharesAllParties[msgPayload.ShareOwnerID] = msgPayload.Values;
                    }
                    Serilog.Log.Verbose($"MpcSharedStorage: Party {msgPayload.ShareOwnerID} exposed BigInteger shares.");
                }
                break;
            case MessagePayloadType.BigIntegerInputShare: {
                    BigIntegerInputShareMessagePayload msgPayload = msg.ExtractMessagePayload<BigIntegerInputShareMessagePayload>();
                    List<IReadOnlyList<BigInteger>?> sharesAllParties = this.ExposedInputShareVectors.GetOrAdd(msgPayload.ExposureKey, _ => this.NewExposedInputShareVectorAllParties());
                    lock (sharesAllParties) {
                        sharesAllParties[msgPayload.ShareOwnerID] = msgPayload.Values;
                    }
                    Serilog.Log.Verbose($"MpcSharedStorage: Party {msgPayload.ShareOwnerID} exposed BigInteger input shares.");
                }
                break;
            case MessagePayloadType.BoolExposure: {
                    BoolExposureMessagePayload msgPayload = msg.ExtractMessagePayload<BoolExposureMessagePayload>();
                    List<bool> bits = msgPayload.Bits.ToList();

                    List<IReadOnlyList<bool>?> sharesAllParties = this.ExposedBoolShareVectors.GetOrAdd(msgPayload.ExposureKey, _ => this.NewExposedBoolShareVectorAllParties());
                    lock (sharesAllParties) {
                        sharesAllParties[msgPayload.ShareOwnerID] = bits;
                    }
                    Serilog.Log.Verbose($"MpcSharedStorage: Party {msgPayload.ShareOwnerID} exposed Bool shares.");
                }
                break;
            default:
                throw new InvalidDataException("Unrecognized payload type.");
        }
    }

}
