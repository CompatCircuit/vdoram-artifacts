namespace Anonymous.CompatCircuitCore.Extensions;
public static class BitsHelper {
    public static string ToDigitString(this IEnumerable<bool> bits) => string.Join(string.Empty, bits.Select(bit => bit ? 1 : 0));
}
