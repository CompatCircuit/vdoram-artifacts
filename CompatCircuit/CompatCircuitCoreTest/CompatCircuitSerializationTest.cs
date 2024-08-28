using SadPencil.CompatCircuitCore.CompatCircuits;

namespace SadPencil.CompatCircuitCoreTest;

[TestClass]
public class CompatCircuitSerializationTest {
    [TestMethod]
    public void TestCompatCircuitSerialization() {

        CompatCircuit circuit = SingleExecutorTest.GetTestCircuit();

        using MemoryStream stream = new();
        CompatCircuitSerializer.Serialize(circuit, stream, leaveOpen: true);
        _ = stream.Seek(0, SeekOrigin.Begin);
        CompatCircuit deserialized = CompatCircuitSerializer.Deserialize(stream);

        Assert.AreEqual(circuit, deserialized);
    }
}
