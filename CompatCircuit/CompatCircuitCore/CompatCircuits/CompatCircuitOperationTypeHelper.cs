namespace SadPencil.CompatCircuitCore.CompatCircuits;
public static class CompatCircuitOperationTypeHelper {
    public static string OperationTypeToString(CompatCircuitOperationType operationType) => operationType switch {
        CompatCircuitOperationType.Addition => "ADD",
        CompatCircuitOperationType.Multiplication => "MUL",
        CompatCircuitOperationType.Inversion => "INV",
        CompatCircuitOperationType.BitDecomposition => "BITS",
        _ => throw new Exception($"Unknown operation type {operationType}"),
    };
}