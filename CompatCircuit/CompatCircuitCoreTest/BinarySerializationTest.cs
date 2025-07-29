using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.BinarySerialization;
using Anonymous.CompatCircuitCore.CompatCircuits;
using Anonymous.CompatCircuitCore.Computation.MultiParty.Messages;
using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.DaBitPrioPlus;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.EdaBitsKai;
using Anonymous.CompatCircuitCore.RandomGenerators;
using System.Collections;
using System.Numerics;

namespace Anonymous.CompatCircuitCoreTest;

[TestClass]
public class BinarySerializationTest {

    private static readonly IReadOnlyList<Field> TestFields = [
        ArithConfig.FieldFactory.Zero,
        ArithConfig.FieldFactory.One,
        ArithConfig.FieldFactory.NegOne,
        ArithConfig.FieldFactory.Two,
        ArithConfig.FieldFactory.New(BigInteger.Parse("114514")),
        ArithConfig.FieldFactory.New(BigInteger.Parse("235560319172772926646703050616099496969237409629580586433170560970816329982")),
    ];

    [TestMethod]
    public void TestDaBitPrioPlusSerialization() {

        int partyCount = 4;
        int daBitPrioPlusCount = 10;

        // Generate daBitPrioPluss
        List<DaBitPrioPlusShareList> daBitPrioPlusShareListForAllParties = DaBitPrioPlusGenerator.GenerateDaBitPrioPlusShareListForAllParties(ArithConfig.FieldFactory, ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator, partyCount, daBitPrioPlusCount);

        // Binary serialization test for DaBitPrioPlusShare
        DaBitPrioPlusShare daBitPrioPlusShare = daBitPrioPlusShareListForAllParties[1][2];
        _ = TestArithFactoryBinarySerialization(daBitPrioPlusShare, ArithConfig.FieldFactory);

        // Binary serialization test for DaBitPrioPlusShareList
        DaBitPrioPlusShareList daBitPrioPlusShareList = daBitPrioPlusShareListForAllParties[0];
        {
            DaBitPrioPlusShareList daBitPrioPlusShareListRecovered = TestArithFactoryBinarySerialization(daBitPrioPlusShareList, ArithConfig.FieldFactory, skipObjectEqualityTest: true);
            Assert.IsTrue(daBitPrioPlusShareList.SequenceEqual(daBitPrioPlusShareListRecovered));
        }

        // Test file enumerator
        using MemoryStream stream = new();
        DaBitPrioPlusShareFileEnumerator.WriteStream(stream, daBitPrioPlusShareList, leaveOpen: true);
        _ = stream.Seek(0, SeekOrigin.Begin);
        {
            List<DaBitPrioPlusShare> daBitPrioPlusShareListRecovered = new DaBitPrioPlusShareFileEnumerator(stream, ArithConfig.FieldFactory).AsEnumerable().ToList();
            Assert.IsTrue(daBitPrioPlusShareList.SequenceEqual(daBitPrioPlusShareListRecovered));
        }
    }

    [TestMethod]
    public void TestEdaBitsSerialization() {

        int partyCount = 4;
        int edaBitsCount = 10;

        // Generate edaBits
        List<EdaBitsKaiShareList> edaBitsShareListForAllParties = EdaBitsKaiGenerator.GenerateEdaBitsShareListForAllParties(ArithConfig.FieldFactory, ArithConfig.FieldSecretSharing, ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator, ArithConfig.BitSize, partyCount, edaBitsCount);

        EdaBitsKaiShare edaBitsShare = edaBitsShareListForAllParties[1][2];
        _ = TestArithFactoryBinarySerialization(edaBitsShare, ArithConfig.FieldFactory);

        // Binary serialization test for EdaBitsShareList
        EdaBitsKaiShareList edaBitsShareList = edaBitsShareListForAllParties[0];
        {
            EdaBitsKaiShareList edaBitsShareListRecovered = TestArithFactoryBinarySerialization(edaBitsShareList, ArithConfig.FieldFactory, skipObjectEqualityTest: true);
            Assert.IsTrue(edaBitsShareList.SequenceEqual(edaBitsShareListRecovered));
        }

        // Test file enumerator
        using MemoryStream stream = new();
        EdaBitsKaiShareFileEnumerator.WriteStream(stream, edaBitsShareList, leaveOpen: true);
        _ = stream.Seek(0, SeekOrigin.Begin);
        {
            List<EdaBitsKaiShare> edaBitsShareListRecovered = new EdaBitsKaiShareFileEnumerator(stream, ArithConfig.FieldFactory).AsEnumerable().ToList();
            Assert.IsTrue(edaBitsShareList.SequenceEqual(edaBitsShareListRecovered));
        }
    }

    [TestMethod]
    public void TestBoolBeaverTripleSerialization() {

        int partyCount = 4;
        int beaverCount = 10;

        // Generate beaver triples
        List<BoolBeaverTripleShareList> beaverShareListForAllParties = BoolBeaverTripleGenerator.GenerateBeaverTripleShareListForAllParties(ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator, partyCount, beaverCount);

        // Binary serialization test for BeaverTripleShare
        BoolBeaverTripleShare tripleShare = beaverShareListForAllParties[1][2];
        _ = TestGeneralBinarySerialization(tripleShare);

        // Binary serialization test for BeaverTripleShareList
        BoolBeaverTripleShareList tripleShareList = beaverShareListForAllParties[0];
        {
            BoolBeaverTripleShareList tripleShareListRecovered = TestGeneralBinarySerialization(tripleShareList, skipObjectEqualityTest: true);
            Assert.IsTrue(tripleShareList.SequenceEqual(tripleShareListRecovered));
        }

        // Test file enumerator
        using MemoryStream stream = new();
        BoolBeaverTripleShareFileEnumerator.WriteStream(stream, tripleShareList, leaveOpen: true);
        _ = stream.Seek(0, SeekOrigin.Begin);
        {
            List<BoolBeaverTripleShare> tripleShareListRecovered = new BoolBeaverTripleShareFileEnumerator(stream).AsEnumerable().ToList();
            Assert.IsTrue(tripleShareList.SequenceEqual(tripleShareListRecovered));
        }
    }

    [TestMethod]
    public void TestFieldBeaverTripleSerialization() {

        int partyCount = 4;
        int beaverCount = 10;

        // Generate beaver triples
        List<FieldBeaverTripleShareList> beaverShareListForAllParties = FieldBeaverTripleGenerator.GenerateBeaverTripleShareListForAllParties(ArithConfig.FieldFactory, partyCount, beaverCount);

        // Binary serialization test for Field
        List<Field> testFields = [.. TestFields, beaverShareListForAllParties[2][3].X, beaverShareListForAllParties[3][7].XY];

        foreach (Field field in testFields) {
            _ = TestArithFactoryBinarySerialization(field, ArithConfig.FieldFactory);
        }

        // Binary serialization test for BeaverTripleShare
        FieldBeaverTripleShare tripleShare = beaverShareListForAllParties[1][2];
        _ = TestArithFactoryBinarySerialization(tripleShare, ArithConfig.FieldFactory);

        // Binary serialization test for BeaverTripleShareList
        FieldBeaverTripleShareList tripleShareList = beaverShareListForAllParties[0];
        {
            FieldBeaverTripleShareList tripleShareListRecovered = TestArithFactoryBinarySerialization(tripleShareList, ArithConfig.FieldFactory, skipObjectEqualityTest: true);
            Assert.IsTrue(tripleShareList.SequenceEqual(tripleShareListRecovered));
        }

        // Test file enumerator
        using MemoryStream stream = new();
        FieldBeaverTripleShareFileEnumerator.WriteStream(stream, tripleShareList, leaveOpen: true);
        _ = stream.Seek(0, SeekOrigin.Begin);
        {
            List<FieldBeaverTripleShare> tripleShareListRecovered = new FieldBeaverTripleShareFileEnumerator(stream, ArithConfig.FieldFactory).AsEnumerable().ToList();
            Assert.IsTrue(tripleShareList.SequenceEqual(tripleShareListRecovered));
        }
    }

    [TestMethod]
    public void TestMpcMessageSerialization() {

        foreach (int nodeID in new List<int>() { 0, 3 }) {
            {
                PartyOnlineMessagePayload payload = new(nodeID);
                PartyOnlineMessagePayload payloadRecovered = TestGeneralBinarySerialization(payload, skipObjectEqualityTest: true);
                Assert.AreEqual(payload.PartyID, payloadRecovered.PartyID);
                TestRawMessage(RawMessageHelper.ComposeRawMessage(sessionID: 0, payload));
            }
            {
                PartyCompletedMessagePayload payload = new(nodeID);
                PartyCompletedMessagePayload payloadRecovered = TestGeneralBinarySerialization(payload, skipObjectEqualityTest: true);
                Assert.AreEqual(payload.PartyID, payloadRecovered.PartyID);
                TestRawMessage(RawMessageHelper.ComposeRawMessage(sessionID: 0, payload));
            }
        }

        string exposedKey = "blah";
        foreach (int shareOwnerID in new List<int>() { 0, 3 }) {
            {
                BigIntegerExposureMessagePayload payload = new(exposedKey, shareOwnerID, values: TestFields.Select(v => v.Value));
                BigIntegerExposureMessagePayload payloadRecovered = TestGeneralBinarySerialization(payload, skipObjectEqualityTest: true);
                Assert.AreEqual(payload.ExposureKey, payloadRecovered.ExposureKey);
                Assert.AreEqual(payload.ShareOwnerID, payloadRecovered.ShareOwnerID);
                Assert.IsTrue(payload.Values.SequenceEqual(payloadRecovered.Values));

                TestRawMessage(RawMessageHelper.ComposeRawMessage(sessionID: 0, payload));
            }
            {
                int bitCount = 29;
                BitArray bits = RandomHelper.RandomBits(bitCount, RandomConfig.RandomGenerator);
                Assert.AreEqual(bitCount, bits.Count);
                BoolExposureMessagePayload payload = new(exposedKey, shareOwnerID, bits);
                BoolExposureMessagePayload payloadRecovered = TestGeneralBinarySerialization(payload, skipObjectEqualityTest: true);
                Assert.AreEqual(payload.ExposureKey, payloadRecovered.ExposureKey);
                Assert.AreEqual(payload.ShareOwnerID, payloadRecovered.ShareOwnerID);
                Assert.IsTrue(payload.Bits.AsEnumerable().SequenceEqual(payloadRecovered.Bits.AsEnumerable()));

                TestRawMessage(RawMessageHelper.ComposeRawMessage(sessionID: 0, payload));
            }

        }
    }

    [TestMethod]
    public void TestCompactCircuitSerialization() {

        CompatCircuit circuit = SingleExecutorTest.GetTestCircuit();
        _ = TestArithFactoryBinarySerialization(circuit, ArithConfig.FieldFactory);
    }

    private static void TestRawMessage(RawMessage obj) {
        RawMessage objRecovered = TestGeneralBinarySerialization(obj, skipObjectEqualityTest: true);
        Assert.AreEqual(obj.MessagePayloadType, objRecovered.MessagePayloadType);
        Assert.IsTrue(obj.MessagePayloadBytes.SequenceEqual(objRecovered.MessagePayloadBytes));
    }

    private static T TestArithFactoryBinarySerialization<T, TArithValue>(T obj, IArithFactory<TArithValue> factory, bool skipObjectEqualityTest = false) where T : IBinaryEncodable, IArithFactoryBinaryDecodable<T, TArithValue> {
        int bytesNeeded = obj.GetEncodedByteCount();
        byte[] buf = new byte[bytesNeeded];
        obj.EncodeBytes(buf, out int bytesWritten);
        Assert.AreEqual(bytesNeeded, bytesWritten);
        T objRecovered = T.FromEncodedBytes(buf, factory, out int bytesRead);
        Assert.AreEqual(bytesWritten, bytesRead);
        if (!skipObjectEqualityTest) {
            Assert.AreEqual(obj, objRecovered);
        }
        return objRecovered;
    }

    private static T TestGeneralBinarySerialization<T>(T obj, bool skipObjectEqualityTest = false) where T : IBinaryEncodable, IGeneralBinaryDecodable<T> {
        int bytesNeeded = obj.GetEncodedByteCount();
        byte[] buf = new byte[bytesNeeded];
        obj.EncodeBytes(buf, out int bytesWritten);
        Assert.AreEqual(bytesNeeded, bytesWritten);
        T objRecovered = T.FromEncodedBytes(buf, out int bytesRead);
        Assert.AreEqual(bytesWritten, bytesRead);
        if (!skipObjectEqualityTest) {
            Assert.AreEqual(obj, objRecovered);
        }
        return objRecovered;
    }
}