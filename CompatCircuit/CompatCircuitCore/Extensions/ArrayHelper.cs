namespace SadPencil.CompatCircuitCore.Extensions;
public static class ArrayHelper {
    public static int FindPattern<T>(T[] haystack, T[] needle) where T : IEquatable<T> {
        ArgumentNullException.ThrowIfNull(haystack);
        ArgumentNullException.ThrowIfNull(needle);

        int len = needle.Length;
        int limit = haystack.Length - len;
        for (int i = 0; i <= limit; i++) {
            int k = 0;
            for (; k < len; k++) {
                if (!Equals(needle[k], haystack[i + k])) {
                    break;
                }
            }
            if (k == len) {
                return i;
            }
        }
        return -1;
    }
}
