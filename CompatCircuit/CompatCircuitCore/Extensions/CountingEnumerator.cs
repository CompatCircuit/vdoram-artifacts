using Anonymous.CompatCircuitCore.Extensions;

public class CountingEnumerator<T> : ICountingEnumerator<T> {
    private readonly IEnumerator<T> _innerEnumerator;
    public long Count { get; private set; }

    public CountingEnumerator(IEnumerator<T> innerEnumerator) {
        this._innerEnumerator = innerEnumerator ?? throw new ArgumentNullException(nameof(innerEnumerator));
        this.Count = 0;
    }

    public T Current => this._innerEnumerator.Current;

    object? System.Collections.IEnumerator.Current => this._innerEnumerator.Current;

    public bool MoveNext() {
        bool notEnded = this._innerEnumerator.MoveNext();
        if (notEnded) {
            this.Count++;
        }

        return notEnded;
    }

    public void Reset() => this._innerEnumerator.Reset();

    public void Dispose() => this._innerEnumerator.Dispose();
}
