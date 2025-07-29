using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.EdaBitsKai;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCoreTest;

[TestClass]
public class MpcBitDecompositionTest {
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(11)]
    [DataRow(17)]
    public void TestMpcBitDecomposition(int partyCount) {
        // Secret field value, to be decomposed into bits
        Field aValue = ArithConfig.FieldFactory.Random();

        // Debug output
        Debug.WriteLine($"aValueAnswer: {aValue.BitDecomposition().ToDigitString()}");

        // Each party has a secret share of the secret value
        List<Field> aShares = ArithConfig.FieldSecretSharing.MakeShares(partyCount, aValue);

        // Generate edaBits
        List<EdaBitsKaiShare> edaBitsShareForAllParties = EdaBitsKaiGenerator.GenerateEdaBitsShareListForAllParties(ArithConfig.FieldFactory, ArithConfig.FieldSecretSharing, ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator, ArithConfig.BitSize, partyCount, 1).Select(list => list[0]).ToList();

        // Output answer for b
        Field bValueAnswer = ArithConfig.FieldSecretSharing.RecoverFromShares(partyCount, Enumerable.Range(0, partyCount).Select(i => edaBitsShareForAllParties[i].ArithShare).ToList());
        Debug.WriteLine($"bValueAnswer: {bValueAnswer.BitDecomposition().ToDigitString()}");

        bool isALessThanBAnswer = aValue.Value < bValueAnswer.Value;

        // Each party computes cShare = aShare - bShare
        List<Field> cShares = [];
        for (int myID = 0; myID < partyCount; myID++) {
            Field aShare = aShares[myID];
            EdaBitsKaiShare edaBitsShare = edaBitsShareForAllParties[myID];
            Field bShare = edaBitsShare.ArithShare;

            Field cShare = aShare - bShare;
            cShares.Add(cShare);
        }

        // Each party reveals C
        Field cValue = ArithConfig.FieldSecretSharing.RecoverFromShares(partyCount, cShares);

        // Debug output
        Debug.WriteLine($"cValue: {cValue.BitDecomposition().ToDigitString()}");

        // Prepare e = C + 2^BitSize - p
        Ring eValue = ArithConfig.ExtRingFactory.New(cValue.Value + ArithConfig.BaseRingFactory.RingSize - ArithConfig.FieldSize);

        // We should use bit adder but for now let's use plaintext values in Ring to simulate (bit adders already passed the test)
        // d = b + c
        Ring dValue = ArithConfig.ExtRingFactory.New(bValueAnswer.Value + cValue.Value);
        // d' = b + e
        Ring dPrimeValue = ArithConfig.ExtRingFactory.New(bValueAnswer.Value) + eValue;

        // q = bool(d>=p)
        bool qValueAnswer = isALessThanBAnswer;
        bool qValueAttempt1 = dValue.Value >= ArithConfig.FieldSize;
        bool qValueAttempt2 = dPrimeValue.BitDecomposition()[ArithConfig.BitSize];
        Assert.AreEqual(qValueAnswer, qValueAttempt1);
        Assert.AreEqual(qValueAnswer, qValueAttempt2);

        // Get NOT q
        bool notQ = !qValueAnswer;

        // h = d - q * p = d + q * (2^BitSize - p)
        Ring hAttempt1 = ArithConfig.BaseRingFactory.NewTruncate((dValue - (qValueAnswer ? ArithConfig.ExtRingFactory.New(ArithConfig.FieldSize) : ArithConfig.ExtRingFactory.Zero)).Value);
        Assert.AreEqual(hAttempt1.Value, aValue.Value);

        // h' = d' + notQ * p
        Ring hPrimeAttempt1 = ArithConfig.BaseRingFactory.NewTruncate((dPrimeValue + (notQ ? ArithConfig.ExtRingFactory.New(ArithConfig.FieldSize) : ArithConfig.ExtRingFactory.Zero)).Value);
        Assert.AreEqual(hPrimeAttempt1.Value, aValue.Value);

        // 6. Convert from boolean share to Field share
        // TODO: implement this step
        // Not implemented. But we have another unit test "MpcExecutorTest" which already covers this test.

    }
}
