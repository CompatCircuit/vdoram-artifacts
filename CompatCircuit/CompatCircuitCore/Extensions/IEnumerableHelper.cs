namespace SadPencil.CompatCircuitCore.Extensions;
public static class IEnumerableHelper {
    public static bool IsIncreasingByOne(this IEnumerable<int> numbers) {
        IEnumerator<int> enumerator = numbers.GetEnumerator();
        if (!enumerator.MoveNext()) {
            return true;
        }
        int previous = enumerator.Current;
        while (enumerator.MoveNext()) {
            if (enumerator.Current != previous + 1) {
                return false;
            }
            previous = enumerator.Current;
        }
        return true;
    }
}
