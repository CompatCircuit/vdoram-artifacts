using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
using Anonymous.CompatCircuitCore.RandomGenerators;
using System.Collections;

namespace Anonymous.CompatCircuitCoreTest;
[TestClass]
public class MpcBeaverTripleTest {
    [TestMethod]
    public void TestFieldBeaverTripleComputation() {

        int partyCount = 3;
        int beaverCount = 1;

        // Generate beaver triples
        List<FieldBeaverTripleShareList> beaverShareListForAllParties = FieldBeaverTripleGenerator.GenerateBeaverTripleShareListForAllParties(ArithConfig.FieldFactory, partyCount, beaverCount);

        // Verify multiplication a * b
        {
            // Create secret shares for a and b
            Field a = ArithConfig.FieldFactory.Random();
            List<Field> aShares = ArithConfig.FieldSecretSharing.MakeShares(partyCount, a);
            Field b = ArithConfig.FieldFactory.Random();
            List<Field> bShares = ArithConfig.FieldSecretSharing.MakeShares(partyCount, b);

            List<Field> cShares = BeaverMulti(aShares, bShares, partyCount, beaverShareListForAllParties, beaverCounter: 0);
            // Recover a * b
            Field cRecovered = ArithConfig.FieldSecretSharing.RecoverFromShares(partyCount, cShares);
            Assert.AreEqual(a * b, cRecovered);
        }

        // Verify inversion
        {
            Field a = ArithConfig.FieldFactory.Random();
            List<Field> aShares = ArithConfig.FieldSecretSharing.MakeShares(partyCount, a);

            IReadOnlyList<bool> fieldSizeMinusTwoBits = (ArithConfig.FieldFactory.NegOne - ArithConfig.FieldFactory.One).BitDecomposition();

            List<Field> baseShare = aShares;
            List<Field>? result = null;
            List<Field> baseToCurrentPower = baseShare;

            for (int i = 0; i < fieldSizeMinusTwoBits.Count; i++) {
                if (fieldSizeMinusTwoBits[i]) {
                    result = result is null
                        ? baseToCurrentPower
                        : BeaverMulti(result, baseToCurrentPower, partyCount, beaverShareListForAllParties, beaverCounter: 0);
                }
                baseToCurrentPower = BeaverMulti(baseToCurrentPower, baseToCurrentPower, partyCount, beaverShareListForAllParties, beaverCounter: 0);
            }

            Assert.IsTrue(result is not null); // Since p - 2 is not zero
            Field resultRecovered = ArithConfig.FieldSecretSharing.RecoverFromShares(partyCount, result);
            Assert.IsTrue(a * resultRecovered == ArithConfig.FieldFactory.One);
        }
    }

    private static List<Field> BeaverMulti(List<Field> aShares, List<Field> bShares, int partyCount, List<FieldBeaverTripleShareList> beaverShareListForAllParties, int beaverCounter) {
        // Each party: dA = [a] - [x]; dB = [b] - [y]; expose dA and dB
        List<Field> dAShares = [];
        List<Field> dBShares = [];
        for (int j = 0; j < partyCount; j++) {
            FieldBeaverTripleShare tripleShare = beaverShareListForAllParties[j][beaverCounter];
            dAShares.Add(aShares[j] - tripleShare.X);
            dBShares.Add(bShares[j] - tripleShare.Y);
        }

        // Each party: recover dA and dB
        Field dA = ArithConfig.FieldSecretSharing.RecoverFromShares(partyCount, dAShares);
        Field dB = ArithConfig.FieldSecretSharing.RecoverFromShares(partyCount, dBShares);

        // Each party: [a * b] = [XY] + dA * [Y] + dB * [X] + dA * dB (note: since it's additive secret sharing, we use dA * [dB] to replace dA * dB)
        List<Field> cShares = [];
        for (int j = 0; j < partyCount; j++) {
            FieldBeaverTripleShare tripleShare = beaverShareListForAllParties[j][beaverCounter];
            cShares.Add(tripleShare.XY + (dA * tripleShare.Y) + (dB * tripleShare.X) + (dA * dBShares[j]));
        }
        return cShares;
    }

    [TestMethod]
    public void TestBoolBeaverTripleComputation() {

        int partyCount = 3;
        int beaverCount = 5;

        // Generate beaver triples
        List<BoolBeaverTripleShareList> beaverShareListForAllParties = BoolBeaverTripleGenerator.GenerateBeaverTripleShareListForAllParties(ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator, partyCount, beaverCount);

        // Verify bitwise AND a & b
        BitArray randomBits = RandomHelper.RandomBits(2 * beaverCount, RandomConfig.RandomGenerator);
        for (int i = 0; i < beaverCount; i++) {
            bool a = randomBits[2 * i];
            bool b = randomBits[(2 * i) + 1];

            // Create secret shares for a and b
            List<bool> aShares = ArithConfig.BoolSecretSharing.MakeShares(partyCount, a);
            List<bool> bShares = ArithConfig.BoolSecretSharing.MakeShares(partyCount, b);

            // Each party: dA = [a] ^ [x]; dB = [b] ^ [y]; expose dA and dB
            List<bool> dAShares = [];
            List<bool> dBShares = [];
            for (int j = 0; j < partyCount; j++) {
                BoolBeaverTripleShare tripleShare = beaverShareListForAllParties[j][i];
                dAShares.Add(aShares[j] ^ tripleShare.X);
                dBShares.Add(bShares[j] ^ tripleShare.Y);
            }

            // Each party: recover dA and dB
            bool dA = ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, dAShares);
            bool dB = ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, dBShares);

            // Each party: [a & b] = [XY] ^ dA & [Y] ^ dB & [X] ^ dA & dB (note: since it's additive secret sharing, we use dA & [dB] to replace dA & dB)
            List<bool> cShares = [];
            for (int j = 0; j < partyCount; j++) {
                BoolBeaverTripleShare tripleShare = beaverShareListForAllParties[j][i];
                cShares.Add(tripleShare.XY ^ (dA & tripleShare.Y) ^ (dB & tripleShare.X) ^ (dA & dBShares[j]));
            }

            // Recover a & b
            bool cRecovered = ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, cShares);
            Assert.AreEqual(a & b, cRecovered);
        }
    }
}
