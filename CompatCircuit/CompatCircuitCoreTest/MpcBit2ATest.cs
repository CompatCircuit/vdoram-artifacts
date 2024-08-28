using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.GlobalConfig;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.DaBitPrioPlus;

namespace SadPencil.CompatCircuitCoreTest;
[TestClass]
public class MpcBit2ATest {
    // Note: we implement the Bit2A conversion as follows.

    // Addanki, Surya, et al. "Prio+: Privacy preserving aggregate statistics via boolean shares."
    // International Conference on Security and Cryptography for Networks. Cham: Springer International Publishing, 2022.
    // Section 8: Share Conversion

    // This conversion protocol is also mentioned in the following paper:

    // Cheng, Nan, Feng Zhang, and Aikaterini Mitrokotsa. "Efficient Three-party Boolean-to-Arithmetic Share Conversion."
    // 2023 20th Annual International Conference on Privacy, Security and Trust (PST). IEEE, 2023.

    // In these schemes the main workload lies in the setup phase, one of them is Prio+ [6],
    // where the authors addressed the Bit2A conversion using daBit [8] prepared in the setup phase.
    // The conversion is illustrated in a semi-honest two-party setting,
    // which transforms [b]_B to [b]_A for a secret input x ∈ Z2:
    // given a random daBit ([r]_B, [r]_A) generated in the setup phase,
    // in the online phase a masked bit δ = r ⊕ b is revealed,
    // then [b]_A is computed as δ ⊕ r = δ + [r]_A − 2δ · [r]_A locally.

    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(6)]
    [DataRow(7)]
    [DataRow(8)]
    public void TestMpcBit2A(int partyCount) {
        // Generate daBits
        int daBitCount = 2; // 2 cases: random bit is true / false
        List<DaBitPrioPlusShareList> daBitPrioPlusShareListForAllParties = DaBitPrioPlusGenerator.GenerateDaBitPrioPlusShareListForAllParties(ArithConfig.FieldFactory, ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator, partyCount, daBitCount);

        foreach (bool randomBit in new bool[] { false, true }) {
            IReadOnlyList<DaBitPrioPlusShare> daBitPrioPlusShareForAllParties = daBitPrioPlusShareListForAllParties.Select(daBitPrioPlusShareList => daBitPrioPlusShareList[randomBit ? 1 : 0]).ToList();

            // Each party initially holds a boolean secret share of the random bit ([b]_B)
            List<bool> bShareAllParties = ArithConfig.BoolSecretSharing.MakeShares(partyCount, randomBit);

            // Each party: compute [d] = [b]_B ^ [r]_B; expose d
            List<bool> deltaShareAllParties = [];
            for (int partyIndex = 0; partyIndex < partyCount; partyIndex++) {
                DaBitPrioPlusShare daBitPrioPlusShare = daBitPrioPlusShareForAllParties[partyIndex];
                bool delta = daBitPrioPlusShare.BoolShare ^ bShareAllParties[partyIndex];
                deltaShareAllParties.Add(delta);
            }

            bool deltaRecovered = ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, deltaShareAllParties);

            // Each party: if delta is false, use [r]_A, otherwise use 1 - [r]_A (this is identical as δ ⊕ r = δ + [r]_A − 2δ · [r]_A)
            List<Field> resultShareAllParties = [];
            for (int partyIndex = 0; partyIndex < partyCount; partyIndex++) {
                DaBitPrioPlusShare daBitPrioPlusShare = daBitPrioPlusShareForAllParties[partyIndex];
                Field constNumberOneShare = partyIndex == 0 ? ArithConfig.FieldFactory.One : ArithConfig.FieldFactory.Zero;
                Field result = !deltaRecovered ? daBitPrioPlusShare.ArithShare : constNumberOneShare - daBitPrioPlusShare.ArithShare;
                resultShareAllParties.Add(result);
            }

            // Check the result
            Field resultRecovered = ArithConfig.FieldSecretSharing.RecoverFromShares(partyCount, resultShareAllParties);
            Field expectedResult = randomBit ? ArithConfig.FieldFactory.One : ArithConfig.FieldFactory.Zero;
            Assert.AreEqual(expectedResult, resultRecovered);
        }
    }

}
