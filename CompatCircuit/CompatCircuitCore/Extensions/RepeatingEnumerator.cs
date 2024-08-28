using System.Collections;

namespace SadPencil.CompatCircuitCore.Extensions;
public class RepeatingEnumerator<T> : IEnumerator<T> {
    private readonly IEnumerator<T> _enumerator;

    public RepeatingEnumerator(IEnumerator<T> enumerator) {
        ArgumentNullException.ThrowIfNull(enumerator);
        this._enumerator = enumerator;
    }

    public T Current => this._enumerator.Current;

    object IEnumerator.Current => this.Current;

    public bool MoveNext() {
        if (!this._enumerator.MoveNext()) {
            // Reset and move next again
            this._enumerator.Reset();
            return this._enumerator.MoveNext();
        }
        return true;
    }

    public void Reset() => this._enumerator.Reset();

    public void Dispose() => this._enumerator.Dispose();
}
