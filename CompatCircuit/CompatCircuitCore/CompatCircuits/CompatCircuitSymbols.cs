namespace SadPencil.CompatCircuitCore.CompatCircuits;
public class CompatCircuitSymbols {
    public required string CircuitName { get; init; }
    public required IReadOnlyList<CompatCircuitWireSymbol> CircuitWireSymbols { get; init; }

    public Dictionary<string, CompatCircuitWireSymbol> GetWireNameToWireSymbolsDictionary() {
        Dictionary<string, CompatCircuitWireSymbol> dict = [];
        foreach (CompatCircuitWireSymbol symbol in this.CircuitWireSymbols) {
            if (symbol.WireName is not null) {
                dict.Add(symbol.WireName, symbol);
            }
        }
        return dict;
    }

    public Dictionary<int, CompatCircuitWireSymbol> GetWireIDToWireSymbolsDictionary() {
        Dictionary<int, CompatCircuitWireSymbol> dict = [];
        foreach (CompatCircuitWireSymbol symbol in this.CircuitWireSymbols) {
            dict.Add(symbol.WireID, symbol);
        }
        return dict;
    }
}
