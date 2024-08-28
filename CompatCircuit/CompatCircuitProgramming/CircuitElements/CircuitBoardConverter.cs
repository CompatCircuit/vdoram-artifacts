using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.CompatCircuits;
using System.Diagnostics;

namespace SadPencil.CompatCircuitProgramming.CircuitElements;
public static class CircuitBoardConverter {
    public static void ToCompatCircuit(CircuitBoard circuitBoard, string circuitName, out CompatCircuit compatCircuit, out CompatCircuitSymbols compatCircuitSymbols)
        => ToCompatCircuit(circuitBoard, circuitName, out compatCircuit, out compatCircuitSymbols, out _);
    public static void ToCompatCircuit(CircuitBoard circuitBoard, string circuitName, out CompatCircuit compatCircuit, out CompatCircuitSymbols compatCircuitSymbols, out CompatCircuitSymbols compatCircuitDebugSymbols) {
        static void Action(CircuitBoard circuitBoard, string circuitName, out CompatCircuit compatCircuit, out CompatCircuitSymbols compatCircuitSymbols, out CompatCircuitSymbols compatCircuitDebugSymbols) {
            List<CompatCircuitWireSymbol> wireSymbols = [];
            List<CompatCircuitWireSymbol> debugSymbols = [];

            IReadOnlyList<Field> reservedConstants = CompatCircuit.ReservedWireValues;
            Dictionary<Field, int> reservedWireIDs = [];
            for (int i = 0; i < reservedConstants.Count; i++) {
                reservedWireIDs.Add(reservedConstants[i], i);
                CompatCircuitWireSymbol wireSymbol = new() {
                    IsConstant = true,
                    IsPublicInput = true,
                    IsPrivateInput = false,
                    IsPrivateOutput = false,
                    IsPublicOutput = false,
                    WireID = i,
                    WireName = $"const_number_{reservedConstants[i]}",
                };
                debugSymbols.Add(wireSymbol);
            }

            Dictionary<Wire, int> wireIDs = [];
            int nextID = CompatCircuit.ReservedWireConstantCount;
            List<Field> constantInputs = [];

            foreach (Wire wire in circuitBoard.ConstantWires) {
                Trace.Assert(wire.ConstValue is not null);
                if (reservedWireIDs.TryGetValue(wire.ConstValue, out int id)) {
                    wireIDs[wire] = id;
                }
                else {
                    wireIDs[wire] = nextID;
                    CompatCircuitWireSymbol wireSymbol = new() {
                        IsConstant = true,
                        IsPublicInput = true,
                        IsPrivateInput = false,
                        IsPrivateOutput = false,
                        IsPublicOutput = false,
                        WireID = nextID,
                        WireName = wire.Name,
                    };
                    debugSymbols.Add(wireSymbol);
                    nextID++;
                    constantInputs.Add(wire.ConstValue);
                }
            }

            int constantWireCount = nextID;

            Trace.Assert(constantWireCount == CompatCircuit.ReservedWireConstantCount + constantInputs.Count);

            foreach (Wire wire in circuitBoard.PublicInputWires) {
                wireIDs[wire] = nextID;
                CompatCircuitWireSymbol wireSymbol = new() {
                    IsConstant = false,
                    IsPublicInput = true,
                    IsPrivateInput = false,
                    IsPrivateOutput = false,
                    IsPublicOutput = false,
                    WireID = nextID,
                    WireName = wire.Name
                };
                wireSymbols.Add(wireSymbol);
                debugSymbols.Add(wireSymbol);
                nextID++;
            }

            int publicInputWireCount = nextID;

            foreach (Wire wire in circuitBoard.PrivateInputWires) {
                wireIDs[wire] = nextID;
                CompatCircuitWireSymbol wireSymbol = new() {
                    IsConstant = false,
                    IsPublicInput = false,
                    IsPrivateInput = true,
                    IsPrivateOutput = false,
                    IsPublicOutput = false,
                    WireID = nextID,
                    WireName = wire.Name
                };
                wireSymbols.Add(wireSymbol);
                debugSymbols.Add(wireSymbol);
                nextID++;
            }

            int inputWireCount = nextID;

            HashSet<int> publicOutputs = [];
            foreach (Operation op in circuitBoard.Operations) {
                foreach (Wire wire in op.OutputWires) {
                    wireIDs[wire] = nextID;

                    if (wire.IsPublicOutput) {
                        _ = publicOutputs.Add(nextID);
                    }

                    CompatCircuitWireSymbol wireSymbol = new() {
                        IsConstant = false,
                        IsPublicInput = false,
                        IsPrivateInput = false,
                        IsPrivateOutput = wire.IsPrivateOutput,
                        IsPublicOutput = wire.IsPublicOutput,
                        WireID = nextID,
                        WireName = wire.Name
                    };
                    debugSymbols.Add(wireSymbol);

                    if (wire.IsPublicOutput || wire.IsPrivateOutput) {
                        wireSymbols.Add(wireSymbol);
                    }

                    nextID++;
                }
            }

            int wireCount = nextID;

            List<CompatCircuitOperation> compatCircuitOperations = [];
            foreach (Operation op in circuitBoard.Operations) {
                CompatCircuitOperationType compatCircuitOperationType = op.OperationType switch {
                    OperationType.Addition => CompatCircuitOperationType.Addition,
                    OperationType.Multiplication => CompatCircuitOperationType.Multiplication,
                    OperationType.Inversion => CompatCircuitOperationType.Inversion,
                    OperationType.BitDecomposition => CompatCircuitOperationType.BitDecomposition,
                    _ => throw new Exception("Unrecognized operation type"),
                };

                compatCircuitOperations.Add(new CompatCircuitOperation(
                     operationType: compatCircuitOperationType,
                     inputWires: op.InputWires.Select(wire => wireIDs[wire]).ToList(),
                     outputWires: op.OutputWires.Select(wire => wireIDs[wire]).ToList()));
            }

            compatCircuit = new CompatCircuit() {
                ConstantInputs = constantInputs,
                ConstantWireCount = constantWireCount,
                PublicInputWireCount = publicInputWireCount,
                InputWireCount = inputWireCount,
                WireCount = wireCount,
                PublicOutputs = publicOutputs,
                Operations = compatCircuitOperations,
            };

            compatCircuitSymbols = new CompatCircuitSymbols() {
                CircuitName = circuitName,
                CircuitWireSymbols = wireSymbols,
            };

            compatCircuitDebugSymbols = new CompatCircuitSymbols() {
                CircuitName = circuitName,
                CircuitWireSymbols = debugSymbols,
            };
        }

        Action(circuitBoard.Optimize(), circuitName, out compatCircuit, out compatCircuitSymbols, out compatCircuitDebugSymbols);
    }

}
