using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.Extensions;
using SadPencil.CompatCircuitCore.GlobalConfig;

namespace SadPencil.CompatCircuitCoreTest;
[TestClass]
public class JsonSerializationTest {
    [TestMethod]
    public void TestFieldJsonSerialization() {

        Field field = ArithConfig.FieldFactory.NegOne;
        string str1 = JsonSerializerHelper.Serialize(field.Value.ToString());
        string str2 = JsonSerializerHelper.Serialize(field.ToString());
        string str3 = JsonSerializerHelper.Serialize(field);

        Assert.AreEqual(str1, str2);
        Assert.AreEqual(str1, str3);
    }

    [TestMethod]
    public void TestFieldListJsonSerialization() {

        List<Field?> fields = [ArithConfig.FieldFactory.NegOne, ArithConfig.FieldFactory.Zero, ArithConfig.FieldFactory.Two, null];
        string left = JsonSerializerHelper.Serialize(fields.Select(v => v ?? null));
        string right = JsonSerializerHelper.Serialize(fields);
        Assert.AreEqual(left, right);
    }
}
