using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.CompatCircuits;
using Anonymous.CompatCircuitCore.CompatCircuits.MpcCircuits;
using Anonymous.CompatCircuitCore.CompatCircuits.R1csCircuits;
using Anonymous.CompatCircuitCore.Computation;
using Anonymous.CompatCircuitCore.Computation.MultiParty;
using Anonymous.CompatCircuitCore.Computation.MultiParty.Network;
using Anonymous.CompatCircuitCore.Computation.MultiParty.SharedStorages;
using Anonymous.CompatCircuitCore.Computation.SingleParty;
using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.DaBitPrioPlus;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.EdaBitsKai;
using System.Text;

namespace Anonymous.CompatCircuitCoreTest;
[TestClass]
public class MpcExecutorTest {
    public static CompatCircuit GetTestCircuit() {
        static MemoryStream MemoryStreamFromString(string value, Encoding encoding) => new(encoding.GetBytes(value ?? string.Empty));

        // This circuit behaves like the one from SingleExecutorTest but the result bits of bit decomposition are not public
        string circuitText = """
; const 0 = 0;
; const 1 = -1;
; const 2 = 1;
; const 3..254 from 2^{id-2};
; const 255 = 11; (quadratic nonresidue)
; const 256 = -11;
reserved 0 .. 256;
const 257 .. 258;
pubin 259 .. 259;
privin 260 .. 261;
total 521;

; output 265 = a * b + c * (d + e) + 4
const 257 = 114514 ; a
const 258 = 1919810 ; d
; pubin 259 ; b (input: 114)
; privin 260 ; c (input: 514)
; privin 261 ; e (input: 1919)
add 262 = 258 + 261; d + e (answer: 1921729)
mul 263 = 257 * 259 ; a * b (answer: 13054596)
mul 264 = 260 * 262 ; c * (d + e)  (answer: 987768706)
add 265 = 263 + 264; a * b + c * (d + e) (answer: 1000823302)
output 265;

; output 266 = inverse of output 265
inv 266 from 265;
output 266;

; wire 267 is not a public output
add 267 = 266 + 260;

; output 268 .. 520 = bit decompositions of 267
bits 268 .. 520 from 267;
""";
        using MemoryStream stream = MemoryStreamFromString(circuitText, EncodingHelper.UTF8Encoding);
        CompatCircuit circuit = CompatCircuitSerializer.Deserialize(stream);
        return circuit;
    }

    [TestMethod]
    public void TestMpcExecutor() {

        int partyCount = 2;

        Serilog.Log.Information("Generate Beaver triples and EdaBits shares...");
        List<FieldBeaverTripleShareList> fieldBeaverTripleShareListForAllParties = FieldBeaverTripleGenerator.GenerateBeaverTripleShareListForAllParties(ArithConfig.FieldFactory, partyCount, beaverCount: 6000);
        List<BoolBeaverTripleShareList> boolBeaverTripleShareListForAllParties = BoolBeaverTripleGenerator.GenerateBeaverTripleShareListForAllParties(ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator, partyCount, beaverCount: 3000);
        List<EdaBitsKaiShareList> edaBitsShareListForAllParties = EdaBitsKaiGenerator.GenerateEdaBitsShareListForAllParties(ArithConfig.FieldFactory, ArithConfig.FieldSecretSharing, ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator, ArithConfig.BitSize, partyCount, 1);
        List<DaBitPrioPlusShareList> daBitPrioPlusShareListForAllParties = DaBitPrioPlusGenerator.GenerateDaBitPrioPlusShareListForAllParties(ArithConfig.FieldFactory, ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator, partyCount, 300);

        // Run SingleExecutor first to get the answer
        Serilog.Log.Information("Run SingleExecutor...");
        CompatCircuitConverter.ToMpcCircuitAndR1csCircuit(GetTestCircuit(), out MpcCircuit mpcCircuit, out R1csCircuit r1csCircuit);
        Dictionary<int, Field> publicInputValueDict = new() {
            {259, ArithConfig.FieldFactory.New(114)},
        };
        Dictionary<int, Field> privateInputValueDictSingleExecutor = new() {
            {260, ArithConfig.FieldFactory.New(514)},
            {261, ArithConfig.FieldFactory.New(1919)},
        };

        SingleExecutor singleExecutor = new();
        CircuitExecuteResult singleExecutorResult = singleExecutor.Compute(mpcCircuit, publicInputValueDict, privateInputValueDictSingleExecutor);

        Dictionary<int, Field> privateInputValueDictParty0 = new() {
            {260, ArithConfig.FieldFactory.New(514)},
        };
        Dictionary<int, Field> privateInputValueDictParty1 = new() {
            {261, ArithConfig.FieldFactory.New(1919)},
        };
        List<Dictionary<int, Field>> privateInputValueDictAllParties = [
            privateInputValueDictParty0,
            privateInputValueDictParty1,
        ];

        Serilog.Log.Information("Run MpcExecutor...");
        IMpcClient mpcClient = new DummyMpcClient();
        MpcSharedStorageSessionManager manager = new(mpcClient);
        IMpcSharedStorage mpcSharedStorage = new MpcSharedStorage(manager, sessionID: 0, partyCount);

        int tickMS = 0;

        List<ICountingEnumerator<FieldBeaverTripleShare>> fieldBeaverTripleShareEnumeratorAllParties = fieldBeaverTripleShareListForAllParties.Select(list => new CountingEnumerator<FieldBeaverTripleShare>(list.GetEnumerator()) as ICountingEnumerator<FieldBeaverTripleShare>).ToList();
        List<ICountingEnumerator<BoolBeaverTripleShare>> boolBeaverTripleShareEnumeratorAllParties = boolBeaverTripleShareListForAllParties.Select(list => new CountingEnumerator<BoolBeaverTripleShare>(list.GetEnumerator()) as ICountingEnumerator<BoolBeaverTripleShare>).ToList();
        List<ICountingEnumerator<EdaBitsKaiShare>> edaBitsShareEnumeratorAllParties = edaBitsShareListForAllParties.Select(list => new CountingEnumerator<EdaBitsKaiShare>(list.GetEnumerator()) as ICountingEnumerator<EdaBitsKaiShare>).ToList();
        List<ICountingEnumerator<DaBitPrioPlusShare>> daBitPrioPlusShareEnumeratorAllParties = daBitPrioPlusShareListForAllParties.Select(list => new CountingEnumerator<DaBitPrioPlusShare>(list.GetEnumerator()) as ICountingEnumerator<DaBitPrioPlusShare>).ToList();

        List<MpcExecutor> mpcExecutors = [];
        for (int myID = 0; myID < partyCount; myID++) {
            MpcExecutorConfig mpcExecutorConfig = new() {
                MyID = myID,
                PartyCount = partyCount,
                TickMS = tickMS,
            };
            mpcExecutors.Add(new MpcExecutor(
                mpcExecutorConfig, mpcSharedStorage,
                fieldBeaverTripleShareEnumeratorAllParties[myID], boolBeaverTripleShareEnumeratorAllParties[myID], edaBitsShareEnumeratorAllParties[myID], daBitPrioPlusShareEnumeratorAllParties[myID]) { LoggerPrefix = $"MPC{myID}" });
        }

        // wire id -> party id -> mpc value share
        IReadOnlyList<IReadOnlyList<MpcValue?>> mpcValuesAllParties;
        {
            // Run MPC Executor
            Task<CircuitExecuteResult>[] tasks = tickMS == 0
                ? Enumerable.Range(0, partyCount)
                    .Select(myID => Task.Run(() => mpcExecutors[myID].Compute(mpcCircuit, publicInputValueDict, privateInputValueDictAllParties[myID])))
                    .ToArray()
                : Enumerable.Range(0, partyCount)
                    .Select(myID => mpcExecutors[myID].Compute(mpcCircuit, publicInputValueDict, privateInputValueDictAllParties[myID]))
                    .ToArray();
            Task.WaitAll(tasks);

            // Collect results
            List<IReadOnlyList<MpcValue?>> mpcValuesAllPartiesList = [];
            for (int i = 0; i < mpcCircuit.WireCount; i++) {
                List<MpcValue?> mpcValueShares = [];
                for (int myID = 0; myID < partyCount; myID++) {
                    mpcValueShares.Add(tasks[myID].Result.ValueBoard[i]);
                }
                mpcValuesAllPartiesList.Add(mpcValueShares);
            }
            mpcValuesAllParties = mpcValuesAllPartiesList;
        }

        // Check for the answer
        foreach (int wireID in mpcCircuit.PublicOutputs) {
            if (mpcValuesAllParties[wireID].Any(v => v is null || v.IsSecretShare || v.Value != singleExecutorResult.PublicOutputs[wireID])) {
                Assert.Fail($"Output wire {wireID} is null, a secret share, or a wrong answer.");
            }
        }

        // Recover from secret shares
        List<MpcValue> circuitValues = [];
        for (int wireID = 0; wireID < r1csCircuit.WireCount; wireID++) {
            bool isNull = mpcValuesAllParties[wireID][0] is null;
            if (mpcValuesAllParties[wireID].Any(v => (v is null) != isNull)) {
                Assert.Fail($"Parties have different behavior about wire {wireID}");
            }

            if (isNull) {
                throw new Exception("Test circuit should not have unsigned wires");
            }

            bool isSecretShare = mpcValuesAllParties[wireID][0]!.IsSecretShare;
            if (mpcValuesAllParties[wireID].Any(v => v!.IsSecretShare != isSecretShare)) {
                Assert.Fail($"Parties have different behavior about wire {wireID}");
            }

            Field recovered;
            if (isSecretShare) {
                recovered = ArithConfig.FieldSecretSharing.RecoverFromShares(partyCount, mpcValuesAllParties[wireID].Select(v => v!.Value));
            }
            else {
                recovered = mpcValuesAllParties[wireID][0]!.Value;
                if (mpcValuesAllParties[wireID].Any(v => v!.Value != recovered)) {
                    Assert.Fail($"Parties have different behavior about wire {wireID}");
                }
            }

            Assert.IsTrue(!singleExecutorResult.ValueBoard[wireID].IsSecretShare);
            Assert.AreEqual(recovered, singleExecutorResult.ValueBoard[wireID].Value);
            circuitValues.Add(new MpcValue(recovered, isSecretShare: false));
        }

        // Print how many beaver triples and EdaBits shares are used
        Serilog.Log.Information($"Field beaver triples used: {fieldBeaverTripleShareEnumeratorAllParties[0].Count}");
        Serilog.Log.Information($"Bool beaver triples used: {boolBeaverTripleShareEnumeratorAllParties[0].Count}");
        Serilog.Log.Information($"edaBits shares used: {edaBitsShareEnumeratorAllParties[0].Count}");
        Serilog.Log.Information($"daBitPrioPlus shares used: {daBitPrioPlusShareEnumeratorAllParties[0].Count}");

        // Also these values should be the same
        for (int partyIndex = 1; partyIndex < partyCount; partyIndex++) {
            Assert.AreEqual(fieldBeaverTripleShareEnumeratorAllParties[0].Count, fieldBeaverTripleShareEnumeratorAllParties[partyIndex].Count);
            Assert.AreEqual(boolBeaverTripleShareEnumeratorAllParties[0].Count, boolBeaverTripleShareEnumeratorAllParties[partyIndex].Count);
            Assert.AreEqual(edaBitsShareEnumeratorAllParties[0].Count, edaBitsShareEnumeratorAllParties[partyIndex].Count);
            Assert.AreEqual(daBitPrioPlusShareEnumeratorAllParties[0].Count, daBitPrioPlusShareEnumeratorAllParties[partyIndex].Count);
        }

        // Check for r1cs constraints
        R1csCircuitWithValues r1CsCircuitWithValues = R1csCircuitWithValues.FromR1csCircuit(r1csCircuit, circuitValues);
        r1CsCircuitWithValues.SelfVerify();
    }
}
