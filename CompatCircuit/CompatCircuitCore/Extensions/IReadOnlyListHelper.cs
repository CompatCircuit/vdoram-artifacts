namespace Anonymous.CompatCircuitCore.Extensions;
public static class IReadOnlyListHelper {
    public static int IndexOf<T>(this IReadOnlyList<T> list, T elementToFind) {
        // https://stackoverflow.com/a/60316143
        // Be aware of the fact that this extension method is not as powerful as a method built into the interface would be.
        // For example, if you are implementing a collection which expects an IEqualityComparer<T> as a construction (or otherwise separate) parameter,
        // this extension method will be blissfully unaware of it, and this will of course lead to bugs.
        int i = 0;
        foreach (T element in list) {
            if (Equals(element, elementToFind)) {
                return i;
            }

            i++;
        }
        return -1;
    }
    public static int FindPattern<T>(IReadOnlyList<T> haystack, IReadOnlyList<T> needle) where T : IEquatable<T> {
        ArgumentNullException.ThrowIfNull(haystack);
        ArgumentNullException.ThrowIfNull(needle);

        int len = needle.Count;
        int limit = haystack.Count - len;
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
