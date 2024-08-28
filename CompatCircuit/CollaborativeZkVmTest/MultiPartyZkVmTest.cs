using SadPencil.CollaborativeZkVm.ZkPrograms;
using SadPencil.CollaborativeZkVm.ZkPrograms.Examples;
using SadPencil.CompatCircuitCore.CompatCircuits.R1csCircuits;
using SadPencil.CompatCircuitCore.Computation;
using SadPencil.CompatCircuitCore.Computation.MultiParty;
using SadPencil.CompatCircuitCore.Computation.MultiParty.SharedStorages;
using SadPencil.CompatCircuitCore.Extensions;
using SadPencil.CompatCircuitCore.GlobalConfig;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.DaBitPrioPlus;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.EdaBitsKai;

namespace SadPencil.CollaborativeZkVmTest;
[TestClass]
public class MultiPartyZkVmTest {
    [TestMethod]
    [DataRow(2)]
    public void TestZkProgramGreaterThanMultiParty(int partyCount) {
        //int fieldBeaverTripleCount = 1000000;
        //int boolBeaverTripleCount = 1000000;
        //int edaBitsPairCount = 1000;
        //int daBitPrioPlusCount = 100000;

        ZkProgramExample zkProgramExample = new GreaterThanZkProgramGenerator().GetZkProgram();
        List<ZkProgramInstance> zkInstances = zkProgramExample.GetZkProgramInstances(partyCount);

        Serilog.Log.Information("Generate Beaver triples and EdaBits shares...");
        List<FieldBeaverTripleShareList> fieldBeaverTripleShareListForAllParties = FieldBeaverTripleGenerator.GenerateBeaverTripleShareListForAllParties(ArithConfig.FieldFactory, partyCount, beaverCount: 10);
        List<BoolBeaverTripleShareList> boolBeaverTripleShareListForAllParties = BoolBeaverTripleGenerator.GenerateBeaverTripleShareListForAllParties(ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator, partyCount, beaverCount: 10);
        List<EdaBitsKaiShareList> edaBitsShareListForAllParties = EdaBitsKaiGenerator.GenerateEdaBitsShareListForAllParties(ArithConfig.FieldFactory, ArithConfig.FieldSecretSharing, ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator, ArithConfig.BitSize, partyCount, edaBitsCount: 10);
        List<DaBitPrioPlusShareList> daBitPrioPlusShareListForAllParties = DaBitPrioPlusGenerator.GenerateDaBitPrioPlusShareListForAllParties(ArithConfig.FieldFactory, ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator, partyCount, daBitPrioPlusCount: 10);

        int tickMS = 0;

        List<ICountingEnumerator<FieldBeaverTripleShare>> fieldBeaverTripleShareEnumeratorAllParties =
            fieldBeaverTripleShareListForAllParties.Select(list =>
            new CountingEnumerator<FieldBeaverTripleShare>(new RepeatingEnumerator<FieldBeaverTripleShare>(list.GetEnumerator())) as ICountingEnumerator<FieldBeaverTripleShare>).ToList();
        List<ICountingEnumerator<BoolBeaverTripleShare>> boolBeaverTripleShareEnumeratorAllParties =
            boolBeaverTripleShareListForAllParties.Select(list =>
            new CountingEnumerator<BoolBeaverTripleShare>(new RepeatingEnumerator<BoolBeaverTripleShare>(list.GetEnumerator())) as ICountingEnumerator<BoolBeaverTripleShare>).ToList();
        List<ICountingEnumerator<EdaBitsKaiShare>> edaBitsShareEnumeratorAllParties =
            edaBitsShareListForAllParties.Select(list =>
            new CountingEnumerator<EdaBitsKaiShare>(new RepeatingEnumerator<EdaBitsKaiShare>(list.GetEnumerator())) as ICountingEnumerator<EdaBitsKaiShare>).ToList();
        List<ICountingEnumerator<DaBitPrioPlusShare>> daBitPrioPlusShareEnumeratorAllParties = daBitPrioPlusShareListForAllParties.Select(list =>
            new CountingEnumerator<DaBitPrioPlusShare>(new RepeatingEnumerator<DaBitPrioPlusShare>(list.GetEnumerator())) as ICountingEnumerator<DaBitPrioPlusShare>).ToList();

        IMpcSharedStorageSessionManager mpcSharedStorageSessionManager = new DummyMpcSharedStorageSessionManager();
        IMpcSharedStorageFactory mpcSharedStorageFactory = new MpcSharedStorageCachedFactory() { ExpireAfterRetrievalCount = partyCount };

        List<ZkProgramExecutor> zkProgramExecutors = [];
        for (int myID = 0; myID < partyCount; myID++) {
            MpcExecutorConfig mpcExecutorConfig = new() {
                MyID = myID,
                PartyCount = partyCount,
                TickMS = tickMS,
            };
            IMpcExecutorFactory mpcExecutorFactory = new MpcExecutorFactory(
                mpcExecutorConfig,
                mpcSharedStorageSessionManager,
                mpcSharedStorageFactory,
                fieldBeaverTripleShareEnumeratorAllParties[myID],
                boolBeaverTripleShareEnumeratorAllParties[myID],
                edaBitsShareEnumeratorAllParties[myID],
                daBitPrioPlusShareEnumeratorAllParties[myID]);

            ZkProgramExecutor zkProgramExecutor = new() {
                ZkProgramInstance = zkInstances[myID],
                MyID = myID,
                MpcExecutorFactory = mpcExecutorFactory,
                IsSingleParty = false,
                OnR1csCircuitWithValuesGenerated = new Progress<(string, R1csCircuitWithValues)>(),
            };
            zkProgramExecutors.Add(zkProgramExecutor);
        }

        Task<ZkProgramExecuteResult>[] zkProgramExecutorTasks = Enumerable.Range(0, partyCount).Select(myID => Task.Run(zkProgramExecutors[myID].Execute)).ToArray();
        Task.WaitAll(zkProgramExecutorTasks);

        List<ZkProgramExecuteResult> results = zkProgramExecutorTasks.Select(t => t.Result).ToList();
        for (int i = 0; i < results.Count; i++) {
            ZkProgramExecuteResult result = results[i];

            Serilog.Log.Information($"==== MPC party {i} ====");

            // Print time cost
            Serilog.Log.Information($"Total time cost: {result.TotalTime.TotalSeconds:F6} seconds");
            Serilog.Log.Information("Step time costs:");
            foreach ((string stepName, TimeSpan timeSpan) in result.StepTimes) {
                Serilog.Log.Information($"{stepName}: {timeSpan.TotalSeconds:F6} seconds");
            }

            // Print how many shares are used
            Serilog.Log.Information($"FieldBeaverTripleShare used: {fieldBeaverTripleShareEnumeratorAllParties[i].Count}");
            Serilog.Log.Information($"BoolBeaverTripleShare used: {boolBeaverTripleShareEnumeratorAllParties[i].Count}");
            Serilog.Log.Information($"EdaBitsKaiShare used: {edaBitsShareEnumeratorAllParties[i].Count}");
            Serilog.Log.Information($"DaBitPrioPlusShare used: {daBitPrioPlusShareEnumeratorAllParties[i].Count}");
        }
    }
}
