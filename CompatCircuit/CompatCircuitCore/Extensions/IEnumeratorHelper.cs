namespace SadPencil.CompatCircuitCore.Extensions;
public static class IEnumeratorHelper {
    public static IEnumerable<T> AsEnumerable<T>(this IEnumerator<T> enumerator) {
        while (enumerator.MoveNext()) {
            yield return enumerator.Current;
        }
    }
    public static T FetchNext<T>(this IEnumerator<T> enumerator) {
        ArgumentNullException.ThrowIfNull(enumerator);

        return enumerator.MoveNext() ? enumerator.Current : throw new InvalidOperationException("No more elements in the enumerator.");
    }
}
