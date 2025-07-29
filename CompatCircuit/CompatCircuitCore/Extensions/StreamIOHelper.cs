namespace Anonymous.CompatCircuitCore.Extensions;
public static class StreamIOHelper {
    public static StreamWriter NewUtf8StreamWriter(Stream stream, int bufferSize = -1, bool leaveOpen = false) => new(stream, EncodingHelper.UTF8Encoding, bufferSize, leaveOpen);
    public static StreamReader NewUtf8StreamReader(Stream stream, int bufferSize = -1, bool leaveOpen = false) => new(stream, EncodingHelper.UTF8Encoding, detectEncodingFromByteOrderMarks: false, bufferSize, leaveOpen);
}
