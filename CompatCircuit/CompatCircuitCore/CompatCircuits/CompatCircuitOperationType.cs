namespace SadPencil.CompatCircuitCore.CompatCircuits;
public enum CompatCircuitOperationType : byte {
    Addition,
    Multiplication,
    Inversion,
    /// <summary>
    /// Get bits from the least significant bit to the most significant bit
    /// </summary>
    BitDecomposition,
}

// TODO: Add Neg operation for optimized performance. In ZK, Neg operation is implemented by multiplication, while in MPC, Neg operation is implemented by subtraction. We left this optimization as a TODO.