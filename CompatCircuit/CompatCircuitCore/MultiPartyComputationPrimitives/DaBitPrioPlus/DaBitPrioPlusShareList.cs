using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.BinarySerialization;
using System.Buffers.Binary;
using System.Diagnostics;

namespace SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.DaBitPrioPlus;
public class DaBitPrioPlusShareList : List<DaBitPrioPlusShare>, IBinaryEncodable, IArithFactoryBinaryDecodable<DaBitPrioPlusShareList, Field> {
    public void EncodeBytes(Span<byte> destination, out int bytesWritten) {
        BinaryPrimitives.WriteInt32LittleEndian(destination, this.Count);
        bytesWritten = sizeof(int);

        for (int i = 0; i < this.Count; i++) {
            DaBitPrioPlusShare value = this[i];
            value.EncodeBytes(destination[bytesWritten..], out int valueBytesWritten);
            bytesWritten += valueBytesWritten;
        }
        Trace.Assert(bytesWritten == this.GetEncodedByteCount());
    }

    public int GetEncodedByteCount() => sizeof(int) + this.Select(value => value.GetEncodedByteCount()).Sum();
    public static DaBitPrioPlusShareList FromEncodedBytes(ReadOnlySpan<byte> buffer, IArithFactory<Field> factory, out int bytesRead) {
        int count = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        DaBitPrioPlusShareList list = [];
        bytesRead = sizeof(int);

        for (int i = 0; i < count; i++) {
            DaBitPrioPlusShare value = DaBitPrioPlusShare.FromEncodedBytes(buffer[bytesRead..], factory, out int valueBytesRead);
            bytesRead += valueBytesRead;
            list.Add(value);
        }

        return list;
    }

}

