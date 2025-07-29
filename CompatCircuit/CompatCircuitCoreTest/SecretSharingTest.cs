using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.SecretSharing;

namespace Anonymous.CompatCircuitCoreTest;
[TestClass]
public class SecretSharingTest {
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    public void TestRingSecretSharing(int partyCount) {
        RingFactory ringFactory = RingTest.RingFactory;
        RingSecretSharing ringSecretSharing = new() { RingFactory = ringFactory };
        Ring ring = ringFactory.Random();
        List<Ring> shares = ringSecretSharing.MakeShares(partyCount, ring);
        Ring recovered = ringSecretSharing.RecoverFromShares(partyCount, shares);
        Assert.AreEqual(ring, recovered);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    public void TestFieldSecretSharing(int partyCount) {
        Field field = ArithConfig.FieldFactory.Random();
        List<Field> shares = ArithConfig.FieldSecretSharing.MakeShares(partyCount, field);
        Field recovered = ArithConfig.FieldSecretSharing.RecoverFromShares(partyCount, shares);
        Assert.AreEqual(field, recovered);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    public void TestBoolSecretSharing(int partyCount) {
        for (int i = 0; i < 2; i++) {
            bool value = i == 0;

            List<bool> shares = ArithConfig.BoolSecretSharing.MakeShares(partyCount, value);
            bool recovered = ArithConfig.BoolSecretSharing.RecoverFromShares(partyCount, shares);
            Assert.AreEqual(value, recovered);
        }
    }
}
