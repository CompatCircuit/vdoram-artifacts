using Anonymous.CollaborativeZkVm.ZkVmCircuits;
using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.CompatCircuits;
using Anonymous.CompatCircuitCore.Computation.MultiParty;
using Anonymous.CompatCircuitCore.Computation.MultiParty.Network;
using Anonymous.CompatCircuitCore.Computation.MultiParty.SharedStorages;
using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.DaBitPrioPlus;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.EdaBitsKai;
using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CollaborativeZkVmTest;
[TestClass]
public class PrivateInputShareCircuitBoardGeneratorTest {
    [TestMethod]
    public void TestPrivateInputShareCircuitBoardGenerator() {

        // Prepare private inputs
        int privateInputCount = 3;
        List<Field> privateInputPlaintexts = Enumerable.Range(0, privateInputCount).Select(_ => ArithConfig.FieldFactory.Random()).ToList();

        List<List<int>> privateInputPlaintextsOwnedByParties = [
            [1],
            [0, 2],
        ];
        int partyCount = privateInputPlaintextsOwnedByParties.Count;

        // Prepare circuit
        CircuitBoard privateInputSharingCircuitBoard = new PrivateInputShareCircuitBoardGenerator(privateInputCount).GetCircuitBoard().Optimize();
        CircuitBoardConverter.ToCompatCircuit(privateInputSharingCircuitBoard, "PrivateInputSharingCircuit", out CompatCircuit? privateInputSharingCompatCircuit, out CompatCircuitSymbols? privateInputSharingCompatCircuitSymbols);

        // Prepare MPC
        Serilog.Log.Information("Generate Beaver triples and EdaBits shares...");
        List<FieldBeaverTripleShareList> fieldBeaverTripleShareListForAllParties = FieldBeaverTripleGenerator.GenerateBeaverTripleShareListForAllParties(ArithConfig.FieldFactory, partyCount, beaverCount: privateInputCount);
        List<BoolBeaverTripleShareList> boolBeaverTripleShareListForAllParties = BoolBeaverTripleGenerator.GenerateBeaverTripleShareListForAllParties(ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator, partyCount, beaverCount: 0);
        List<EdaBitsKaiShareList> edaBitsShareListForAllParties = EdaBitsKaiGenerator.GenerateEdaBitsShareListForAllParties(ArithConfig.FieldFactory, ArithConfig.FieldSecretSharing, ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator, ArithConfig.BitSize, partyCount, 0);
        List<DaBitPrioPlusShareList> daBitPrioPlusShareListForAllParties = DaBitPrioPlusGenerator.GenerateDaBitPrioPlusShareListForAllParties(ArithConfig.FieldFactory, ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator, partyCount, 0);

        IMpcClient mpcClient = new DummyMpcClient();
        MpcSharedStorageSessionManager manager = new(mpcClient);
        IMpcSharedStorage mpcSharedStorage = new MpcSharedStorage(manager, sessionID: 0, partyCount);

        List<MpcExecutor> mpcExecutors = [];
        for (int myID = 0; myID < partyCount; myID++) {
            MpcExecutorConfig mpcExecutorConfig = new() {
                MyID = myID,
                PartyCount = partyCount,
                TickMS = 0,
            };
            mpcExecutors.Add(new MpcExecutor(
                mpcExecutorConfig, mpcSharedStorage, fieldBeaverTripleShareListForAllParties[myID], boolBeaverTripleShareListForAllParties[myID], edaBitsShareListForAllParties[myID], daBitPrioPlusShareListForAllParties[myID]) { LoggerPrefix = $"MPC{myID}" });
        }

        List<CircuitBoardMpcWithPartialPrivateInputExecutorWrapper> circuitBoardExecutorWrapperAllParties =
            Enumerable.Range(0, partyCount).Select(partyIndex => new CircuitBoardMpcWithPartialPrivateInputExecutorWrapper(privateInputSharingCompatCircuit, privateInputSharingCompatCircuitSymbols, mpcExecutors[partyIndex])).ToList();

        // Specify inputs
        for (int partyIndex = 0; partyIndex < partyCount; partyIndex++) {
            foreach (int inputIndex in privateInputPlaintextsOwnedByParties[partyIndex]) {
                circuitBoardExecutorWrapperAllParties[partyIndex].AddPrivate($"input_{inputIndex}", privateInputPlaintexts[inputIndex]);
            }
        }

        // Run MPC Executor
        Serilog.Log.Information("Run MPC Executor...");
        Task[] tasks = Enumerable.Range(0, partyCount).Select(myID => Task.Run(() => circuitBoardExecutorWrapperAllParties[myID].Compute())).ToArray();
        Task.WaitAll(tasks);

        // Check outputs
        for (int inputIndex = 0; inputIndex < privateInputCount; inputIndex++) {
            List<MpcValue> shares = Enumerable.Range(0, partyCount).Select(partyIndex => circuitBoardExecutorWrapperAllParties[partyIndex].GetOutput($"output_{inputIndex}")).ToList();
            Assert.IsTrue(shares.All(share => share.IsSecretShare));
            Field recovered = ArithConfig.FieldSecretSharing.RecoverFromShares(partyCount, shares.Select(v => v.Value));
            Assert.AreEqual(privateInputPlaintexts[inputIndex], recovered);
        }
    }
}
