namespace Anonymous.CompatCircuitCore.Extensions;
public static class ListHelper {
    public static void RemoveBySwap<T>(this List<T> list, int index) {
        // https://stackoverflow.com/a/30579982
        list[index] = list[^1];
        list.RemoveAt(list.Count - 1);
    }
}
