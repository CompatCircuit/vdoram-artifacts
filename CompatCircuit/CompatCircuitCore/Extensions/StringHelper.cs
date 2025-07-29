using Microsoft.CodeAnalysis.CSharp;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.Extensions;
public static class StringHelper {
    public static string ToLiteral(this string input) =>
        // https://stackoverflow.com/a/55798623
        SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(input)).ToFullString();

    public static string FromBytes(ReadOnlySpan<byte> buffer, out int bytesRead) {
        bytesRead = 0;

        int length = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        bytesRead += sizeof(int);

        string str = EncodingHelper.UTF8Encoding.GetString(buffer.Slice(bytesRead, length));
        bytesRead += length;

        Trace.Assert(bytesRead == str.GetWriteByteCount());
        return str;
    }
    public static int GetWriteByteCount(this string str) => sizeof(int) + EncodingHelper.UTF8Encoding.GetByteCount(str);
    public static void WriteBytes(this string str, Span<byte> destination, out int bytesWritten) {
        bytesWritten = 0;

        byte[] buffer = EncodingHelper.UTF8Encoding.GetBytes(str);
        BinaryPrimitives.WriteInt32LittleEndian(destination[bytesWritten..], buffer.Length);
        bytesWritten += sizeof(int);

        buffer.CopyTo(destination[bytesWritten..]);
        bytesWritten += buffer.Length;

        Trace.Assert(str.GetWriteByteCount() == bytesWritten);
    }
}
