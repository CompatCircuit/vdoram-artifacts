using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.SecretSharing;
using Anonymous.CompatCircuitCore.RandomGenerators;
using System.Collections;
using System.Numerics;

namespace Anonymous.CompatCircuitCoreTest;
[TestClass]
public class MpcBitAdderTest {
    public static List<List<bool>> GetRandomBitsShareForAllParites(int bitCount, int partyCount) {
        BitArray randomBits = RandomHelper.RandomBits(bitCount, RandomConfig.RandomGenerator);
        return randomBits.AsEnumerable().Select(bit => ArithConfig.BoolSecretSharing.MakeShares(partyCount, bit)).ToList();
    }

    public static IEnumerable<bool> RecoverBits<TBoolList>(IEnumerable<TBoolList> list, int partyCount) where TBoolList : IEnumerable<bool> =>
        list.Select(list => ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, list));

    [TestMethod]
    public void LowBitTest() {

        int partyCount = 2;
        List<List<bool>> input = GetRandomBitsShareForAllParites(ArithConfig.BitSize, partyCount);
        int? firstOneIndex = null;
        for (int i = 0; i < input.Count; i++) {
            bool recoveredBit = ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, input[i]);
            if (recoveredBit) {
                firstOneIndex ??= i;
            }
        }

        List<List<bool>> lowBitResult = LowBit(input, partyCount);
        int? firstOneIndexAfterLowBit = null;
        for (int i = 0; i < input.Count; i++) {
            bool recoveredBit = ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, lowBitResult[i]);
            if (recoveredBit) {
                if (firstOneIndexAfterLowBit is null) {
                    firstOneIndexAfterLowBit = i;
                }
                else {
                    Assert.Fail();
                }
            }
        }

        Assert.AreEqual(firstOneIndex, firstOneIndexAfterLowBit);
    }

    public static List<List<bool>> LowBit<TBoolList>(IReadOnlyList<TBoolList> bits, int partyCount) where TBoolList : IList<bool> {
        static List<List<bool>> Func(IReadOnlyList<IReadOnlyList<bool>> bits, int partyCount) {
            // int lowBit(int a) => a & (~a + 1)
            int bitCount = bits.Count;

            List<bool> one = Enumerable.Repeat(false, bitCount).ToList();
            one[0] = true;

            // bitIndex -> partyIndex
            List<List<bool>> notA = bits.Select(list => BitwiseNot(list, partyCount)).ToList();

            // bitIndex -> partyIndex
            List<List<bool>> negA = BitsAddConst(notA, one, partyCount);
            Assert.AreEqual(bitCount, negA.Count);

            // bitIndex -> partyIndex
            List<List<bool>> ret = [];
            for (int i = 0; i < bitCount; i++) {
                ret.Add(BeaverBitwiseAnd(bits[i], negA[i], partyCount));
            }
            return ret;
        }

        return Func(bits.Select(list => list.AsReadOnly()).ToList(), partyCount);
    }

    [TestMethod]
    public void TestBeaverBitwiseAndOr() {

        int partyCount = 2;
        List<List<bool>> inputBitsAllParties = GetRandomBitsShareForAllParites(2, partyCount);
        List<bool> leftAllParties = inputBitsAllParties[0];
        List<bool> rightAllParties = inputBitsAllParties[1];
        List<bool> andAllParties = BeaverBitwiseAnd(leftAllParties, rightAllParties, partyCount);
        List<bool> orAllParties = BeaverBitwiseOr(leftAllParties, rightAllParties, partyCount);

        for (int i = 0; i < ArithConfig.BitSize; i++) {
            bool left = ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, leftAllParties);
            bool right = ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, rightAllParties);
            bool and = ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, andAllParties);
            bool or = ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, orAllParties);
            Assert.AreEqual(left & right, and);
            Assert.AreEqual(left | right, or);
        }
    }

    public static List<bool> BeaverBitwiseAnd(IReadOnlyList<bool> aShares, IReadOnlyList<bool> bShares, int partyCount) {
        Assert.AreEqual(partyCount, aShares.Count);
        Assert.AreEqual(partyCount, bShares.Count);

        BoolBeaverTripleGenerator boolBeaverTripleGenerator = new() { BoolSecretSharing = ArithConfig.BoolSecretSharing, RandomGenerator = RandomConfig.RandomGenerator };
        IReadOnlyList<BoolBeaverTripleShareList> beaverShareListForAllParties = boolBeaverTripleGenerator.GenerateBeaverTripleShareListForAllParties(partyCount, beaverCount: 2 * 1);
        int beaverCounter = 0;

        // Each party: dA = [a] ^ [x]; dB = [b] ^ [y]; expose dA and dB
        List<bool> dAShares = [];
        List<bool> dBShares = [];
        for (int j = 0; j < partyCount; j++) {
            BoolBeaverTripleShare tripleShare = beaverShareListForAllParties[j][beaverCounter];
            dAShares.Add(aShares[j] ^ tripleShare.X);
            dBShares.Add(bShares[j] ^ tripleShare.Y);
        }

        // Each party: recover dA and dB
        bool dA = ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, dAShares);
        bool dB = ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, dBShares);

        // Each party: [a & b] = [XY] ^ dA & [Y] ^ dB & [X] ^ dA & dB (note: since it's additive secret sharing, we use dA & [dB] to replace dA & dB)
        List<bool> cShares = [];
        for (int j = 0; j < partyCount; j++) {
            BoolBeaverTripleShare tripleShare = beaverShareListForAllParties[j][beaverCounter];
            cShares.Add(tripleShare.XY ^ (dA & tripleShare.Y) ^ (dB & tripleShare.X) ^ (dA & dBShares[j]));
        }
        return cShares;
    }

    public static List<bool> BeaverBitwiseOr(IReadOnlyList<bool> aShares, IReadOnlyList<bool> bShares, int partyCount) {
        Assert.AreEqual(partyCount, aShares.Count);
        Assert.AreEqual(partyCount, bShares.Count);

        List<bool> aAndBShares = BeaverBitwiseAnd(aShares, bShares, partyCount);
        List<bool> resultShares = [];
        for (int i = 0; i < partyCount; i++) {
            resultShares.Add(aShares[i] ^ bShares[i] ^ aAndBShares[i]);
        }
        return resultShares;
    }

    public static List<bool> BitwiseXor(IReadOnlyList<bool> aShares, IReadOnlyList<bool> bShares, int partyCount) {
        Assert.AreEqual(partyCount, aShares.Count);
        Assert.AreEqual(partyCount, bShares.Count);
        List<bool> ret = [];
        for (int myID = 0; myID < partyCount; myID++) {
            ret.Add(aShares[myID] ^ bShares[myID]);
        }
        return ret;
    }

    public static List<bool> BitwiseNot(IReadOnlyList<bool> shares, int partyCount) {
        Assert.AreEqual(partyCount, shares.Count);
        // Only perform bitwise NOT for party 0
        List<bool> resultShares = [];
        resultShares.Add(!shares[0]);
        for (int i = 1; i < partyCount; i++) {
            resultShares.Add(shares[i]);
        }
        return resultShares;
    }

    public static List<List<bool>> BitsAddConst<TBoolList>(IReadOnlyList<TBoolList> leftBitsSharesAllParties, IReadOnlyList<bool> rightBitsPublic, int partyCount) where TBoolList : IList<bool> {
        static List<List<bool>> Func(IReadOnlyList<IReadOnlyList<bool>> leftBitsSharesAllParties, IReadOnlyList<bool> rightBitsPublic, int partyCount) {
            int bitCount = leftBitsSharesAllParties.Count;

            Assert.IsTrue(leftBitsSharesAllParties.All(shares => shares.Count == partyCount));
            Assert.AreEqual(bitCount, rightBitsPublic.Count);

            List<List<bool>> resultShares = [];
            List<bool> carry = Enumerable.Repeat(false, partyCount).ToList();

            for (int i = 0; i < bitCount; i++) {
                List<bool> resultBit = [];
                IReadOnlyList<bool> aBits = leftBitsSharesAllParties[i];
                bool bBit = rightBitsPublic[i];

                for (int myID = 0; myID < partyCount; myID++) {
                    if (myID == 0) {
                        resultBit.Add(aBits[myID] ^ bBit ^ carry[myID]);
                    }
                    else {
                        resultBit.Add(aBits[myID] ^ carry[myID]);
                    }
                }
                resultShares.Add(resultBit);

                if (!bBit) {
                    List<bool> t2 = BeaverBitwiseAnd(aBits, carry, partyCount);
                    carry = t2;
                }
                else {
                    IReadOnlyList<bool> t1 = aBits;
                    IReadOnlyList<bool> t2 = BeaverBitwiseAnd(aBits, carry, partyCount);
                    IReadOnlyList<bool> t3 = carry;
                    IReadOnlyList<bool> t4 = BeaverBitwiseOr(t1, t2, partyCount);
                    List<bool> t5 = BeaverBitwiseOr(t4, t3, partyCount);
                    carry = t5;
                }
            }

            // Drop the last carry bit

            return resultShares;
        }

        return Func(leftBitsSharesAllParties.Select(list => list.AsReadOnly()).ToList(), rightBitsPublic, partyCount);
    }

    public static List<List<bool>> BitsAdd<TBoolList>(IReadOnlyList<TBoolList> leftBitsSharesAllParties, IReadOnlyList<TBoolList> rightBitsSharesAllParties, int partyCount) where TBoolList : IList<bool> {
        static List<List<bool>> Func(IReadOnlyList<IReadOnlyList<bool>> leftBitsSharesAllParties, IReadOnlyList<IReadOnlyList<bool>> rightBitsSharesAllParties, int partyCount) {

            Assert.AreEqual(leftBitsSharesAllParties.Count, rightBitsSharesAllParties.Count);
            int bitCount = leftBitsSharesAllParties.Count;

            Assert.IsTrue(leftBitsSharesAllParties.All(shares => shares.Count == partyCount));
            Assert.IsTrue(rightBitsSharesAllParties.All(shares => shares.Count == partyCount));

            List<List<bool>> resultShares = [];
            List<bool> carry = Enumerable.Repeat(false, partyCount).ToList();

            for (int i = 0; i < bitCount; i++) {
                List<bool> resultBit = [];
                IReadOnlyList<bool> aBits = leftBitsSharesAllParties[i];
                IReadOnlyList<bool> bBits = rightBitsSharesAllParties[i];

                for (int myID = 0; myID < partyCount; myID++) {
                    resultBit.Add(aBits[myID] ^ bBits[myID] ^ carry[myID]);
                }
                resultShares.Add(resultBit);

                IReadOnlyList<bool> t1 = BeaverBitwiseAnd(aBits, bBits, partyCount);
                IReadOnlyList<bool> t2 = BeaverBitwiseAnd(aBits, carry, partyCount);
                IReadOnlyList<bool> t3 = BeaverBitwiseAnd(bBits, carry, partyCount);
                IReadOnlyList<bool> t4 = BeaverBitwiseOr(t1, t2, partyCount);
                List<bool> t5 = BeaverBitwiseOr(t4, t3, partyCount);

                carry = t5;
            }

            // Drop the last carry bit

            return resultShares;
        }

        return Func(leftBitsSharesAllParties.Select(list => list.AsReadOnly()).ToList(), rightBitsSharesAllParties.Select(list => list.AsReadOnly()).ToList(), partyCount);
    }

    [TestMethod]
    public void TestNBitAddOne() {

        int ringBitSize = ArithConfig.BitSize;
        RingFactory ringFactory = new(BigInteger.Pow(2, ringBitSize), RandomConfig.RandomGenerator);

        int partyCount = 5;

        Ring a = ringFactory.Random();
        bool[] aBits = a.BitDecomposition();
        List<List<bool>> aBitsSharedAllParties = [];
        for (int i = 0; i < ringBitSize; i++) {
            aBitsSharedAllParties.Add(ArithConfig.BoolSecretSharing.MakeShares(partyCount, aBits[i]));
        }

        Ring b = ringFactory.One;
        bool[] bBits = b.BitDecomposition();

        List<List<bool>> aPlusBSharedAllParties;
        aPlusBSharedAllParties = BitsAddConst(aBitsSharedAllParties, bBits.ToList(), partyCount);
        List<bool> aPlusBBitsRecovered = [];
        for (int i = 0; i < aPlusBSharedAllParties.Count; i++) {
            aPlusBBitsRecovered.Add(ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, aPlusBSharedAllParties[i]));
        }

        Ring aPlusB = ringFactory.FromBitDecomposition(aPlusBBitsRecovered.ToArray());
        Assert.AreEqual(a + ringFactory.One, aPlusB);
    }

    [TestMethod]
    public void TestNBitAdder() {

        int ringBitSize = ArithConfig.BitSize;
        RingFactory ringFactory = new(BigInteger.Pow(2, ringBitSize), RandomConfig.RandomGenerator);

        Ring a = ringFactory.Random();
        Ring b = ringFactory.Random();
        Ring c = a + b;
        bool[] aBits = a.BitDecomposition();
        bool[] bBits = b.BitDecomposition();
        bool[] cBits = c.BitDecomposition();

        int partyCount = 5;

        List<List<bool>> aBitsSharedAllParties = [];
        for (int i = 0; i < ringBitSize; i++) {
            aBitsSharedAllParties.Add(ArithConfig.BoolSecretSharing.MakeShares(partyCount, aBits[i]));
        }

        List<List<bool>> bBitsSharedAllParties = [];
        for (int i = 0; i < ringBitSize; i++) {
            bBitsSharedAllParties.Add(ArithConfig.BoolSecretSharing.MakeShares(partyCount, bBits[i]));
        }

        for (int caseID = 0; caseID < 2; caseID++) {
            List<List<bool>> aPlusBSharedAllParties = caseID == 0
                ? BitsAdd(aBitsSharedAllParties, bBitsSharedAllParties, partyCount)
                : BitsAddConst(aBitsSharedAllParties, bBits.ToList(), partyCount);
            List<bool> aPlusBRecovered = [];
            for (int i = 0; i < aPlusBSharedAllParties.Count; i++) {
                aPlusBRecovered.Add(ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, aPlusBSharedAllParties[i]));
            }

            Assert.AreEqual(ringBitSize, aPlusBRecovered.Count);
            for (int i = 0; i < ringBitSize; i++) {
                Assert.AreEqual(cBits[i], aPlusBRecovered[i]);
            }
        }

    }

    [TestMethod]
    public void TestNBitAdderWrongMethod() {

        int ringBitSize = ArithConfig.BitSize;
        RingFactory ringFactory = new(BigInteger.Pow(2, ringBitSize), RandomConfig.RandomGenerator);
        RingSecretSharing ringSecretSharing = new() { RingFactory = ringFactory };

        Ring a = ringFactory.Random();
        Ring b = ringFactory.Random();
        Ring c = a + b;
        bool[] cBitsAnswer = c.BitDecomposition();

        int partyCount = 2;

        List<Ring> aShares = ringSecretSharing.MakeShares(partyCount, a);
        List<Ring> bShares = ringSecretSharing.MakeShares(partyCount, b);

        List<List<bool>> cShareBits = [];
        for (int i = 0; i < cBitsAnswer.Length; i++) {
            cShareBits.Add(Enumerable.Repeat(false, partyCount).ToList());
        }

        for (int myID = 0; myID < aShares.Count; myID++) {
            bool[] bits = (aShares[myID] + bShares[myID]).BitDecomposition();
            Assert.AreEqual(cBitsAnswer.Length, bits.Length);
            for (int i = 0; i < bits.Length; i++) {
                cShareBits[i][myID] = bits[i];
            }
        }

        bool[] cBitsRecovered = new bool[cShareBits.Count];
        for (int i = 0; i < cBitsAnswer.Length; i++) {
            cBitsRecovered[i] = ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, cShareBits[i]);
        }

        Assert.IsFalse(cBitsAnswer.SequenceEqual(cBitsRecovered)); // There's a small chance to be true, especially when the bit size is small.
    }
}
