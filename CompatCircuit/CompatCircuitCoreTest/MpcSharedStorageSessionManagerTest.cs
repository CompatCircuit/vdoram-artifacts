using Anonymous.CompatCircuitCore.Computation.MultiParty.Messages;
using Anonymous.CompatCircuitCore.Computation.MultiParty.Network;
using Anonymous.CompatCircuitCore.Computation.MultiParty.SharedStorages;

namespace Anonymous.CompatCircuitCoreTest;
[TestClass]
public class MpcSharedStorageSessionManagerTest {
    [TestMethod]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(8)]
    [DataRow(16)]
    public void TestMpcSharedStorageSessionManagerConcurrencySafe(int partyCount) {
        IMpcSharedStorageSessionManager manager = new MpcSharedStorageSessionManager(new DummyMpcClient());

        void SendPartyOnlineMessage(int sessionID) {
            List<RawMessage> messages = Enumerable.Range(0, partyCount)
                .AsParallel()
                .Select(partyIndex => RawMessageHelper.ComposeRawMessage(sessionID, new PartyOnlineMessagePayload(partyIndex)))
                .ToList();

            Assert.IsTrue(messages.All(v => v is not null));

            messages.AsParallel()
                .ForAll(manager.HandleRawMessage);
        }

        IMpcSharedStorage? sharedStorage = null;

        void RegisterSession(int sessionID) {
            sharedStorage?.UnregisterSession();

            sharedStorage = new MpcSharedStorage(manager, sessionID, partyCount);
        }

        int sessionIDMax = 1000;
        for (int sessionID = 0; sessionID < sessionIDMax; sessionID++) {
            Task sendTask = Task.Run(() => SendPartyOnlineMessage(sessionID));
            Task registerTask = Task.Run(() => RegisterSession(sessionID));
            Task.WaitAll(sendTask, registerTask);

            IReadOnlyList<bool> partyOnline = sharedStorage!.GetPartyOnlineAllParties();
            Assert.IsTrue(partyOnline.All(v => v));
        }

    }
}
