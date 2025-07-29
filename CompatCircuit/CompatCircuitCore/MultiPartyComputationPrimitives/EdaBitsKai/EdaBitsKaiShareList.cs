using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.BinarySerialization;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.EdaBitsKai;
public class EdaBitsKaiShareList : List<EdaBitsKaiShare>, IBinaryEncodable, IArithFactoryBinaryDecodable<EdaBitsKaiShareList, Field> {
    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        BinaryPrimitives.WriteInt32LittleEndian(destination, this.Count);
        bytesWritten = sizeof(int);

        for (int i = 0; i < this.Count; i++) {
            EdaBitsKaiShare value = this[i];
            value.EncodeBytes(destination[bytesWritten..], out int valueBytesWritten);
            bytesWritten += valueBytesWritten;
        }
        Trace.Assert(bytesWritten == this.GetEncodedByteCount());
    }

    public int GetEncodedByteCount() => sizeof(int) + this.Select(value => value.GetEncodedByteCount()).Sum();
    public static EdaBitsKaiShareList FromEncodedBytes(ReadOnlySpan<byte> buffer, IArithFactory<Field> factory, out int bytesRead) {
        int count = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        EdaBitsKaiShareList list = [];
        bytesRead = sizeof(int);

        for (int i = 0; i < count; i++) {
            EdaBitsKaiShare value = EdaBitsKaiShare.FromEncodedBytes(buffer[bytesRead..], factory, out int valueBytesRead);
            bytesRead += valueBytesRead;
            list.Add(value);
        }

        return list;
    }

}
