namespace Anonymous.CompatCircuitProgramming.CircuitElements;
public enum OperationType : byte {
    Addition,
    Multiplication,
    Inversion,
    /// <summary>
    /// Get bits from the least significant bit to the most significant bit
    /// </summary>
    BitDecomposition,
}
