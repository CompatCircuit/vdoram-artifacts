using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.Extensions;
using System.Diagnostics;

namespace SadPencil.CompatCircuitProgramming.CircuitElements;
public class CircuitBoard {
    /// <summary>
    /// This HashSet stores all wires (including operation result wires) to make sure no wires are accidently added twice
    /// </summary>
    private HashSet<Wire> AllWiresHashSet { get; } = [];
    /// <summary>
    /// This HashSet stores all operations to make sure no operations are accidently added twice
    /// </summary>
    private HashSet<Operation> AllOperationsHashSet { get; } = [];

    private HashSet<string> AllWireNamesHashSet { get; } = [];

    private readonly List<Wire> _constantWires = [];
    public IReadOnlyList<Wire> ConstantWires => this._constantWires;

    private readonly List<Wire> _publicInputWires = [];
    public IReadOnlyList<Wire> PublicInputWires => this._publicInputWires;

    private readonly List<Wire> _privateInputWires = [];
    public IReadOnlyList<Wire> PrivateInputWires => this._privateInputWires;

    public IEnumerable<Wire> OperationResultWires => this.Operations.SelectMany(op => op.OutputWires);

    public IEnumerable<Wire> Wires => this.ConstantWires.Concat(this.PublicInputWires).Concat(this.PrivateInputWires).Concat(this.OperationResultWires);
    public int WireCount => this.ConstantWires.Count + this.PublicInputWires.Count + this.PrivateInputWires.Count + this.Operations.Select(op => op.OutputWires.Count).Sum();

    private readonly List<Operation> _operations = [];
    public IReadOnlyList<Operation> Operations => this._operations;
    public int OperationCount => this.Operations.Count;

    public CircuitBoard() { }

    public void AddWire(Wire wire) {
        if (this.AllWiresHashSet.Contains(wire)) {
            throw new InvalidOperationException($"Wire already exists");
        }
        if (wire.WireType == WireType.OperationResult) {
            throw new ArgumentException("Wire of type OperationResult should be added via AddOperation()", nameof(wire));
        }

        if (wire.Name is not null) {
            if (this.AllWireNamesHashSet.Contains(wire.Name)) {
                throw new InvalidOperationException("Wire name already exists");
            }
        }

        _ = this.AllWiresHashSet.Add(wire);

        if (wire.Name is not null) {
            _ = this.AllWireNamesHashSet.Add(wire.Name);
        }

        switch (wire.WireType) {
            case WireType.Constant:
                Trace.Assert(wire.ConstValue is not null);
                this._constantWires.Add(wire); break;
            case WireType.PrivateInput:
                Trace.Assert(wire.ConstValue is null);
                this._privateInputWires.Add(wire); break;
            case WireType.PublicInput:
                Trace.Assert(wire.ConstValue is null);
                this._publicInputWires.Add(wire); break;
            default:
                throw new ArgumentException("Unexpected wire type.", nameof(wire));
        }
    }

    public void AddOperation(Operation operation) {
        foreach (Wire wire in operation.InputWires) {
            if (!this.AllWiresHashSet.Contains(wire)) {
                throw new InvalidOperationException("Unknown input wire");
            }
        }

        foreach (Wire wire in operation.OutputWires) {
            if (this.AllWiresHashSet.Contains(wire)) {
                throw new InvalidOperationException("Output wire already exists");
            }
            if (wire.WireType != WireType.OperationResult) {
                throw new ArgumentException("Output wire should be of type OperationResult", nameof(operation));
            }
        }

        if (this.AllOperationsHashSet.Contains(operation)) {
            throw new InvalidOperationException($"Operation already exists");
        }

        this._operations.Add(operation);
        _ = this.AllOperationsHashSet.Add(operation);
        foreach (Wire wire in operation.OutputWires) {
            _ = this.AllWiresHashSet.Add(wire);
        }
    }

    public CircuitBoard Optimize() {
        static CircuitBoard Func(List<Wire> constantWires, List<Wire> publicInputWires, List<Wire> privateInputWires, List<Operation> operations) {
            // Warning: Bit decomposition output wire IDs should be sequential. This is a requirement from CompatCircuitSerializer.

            // Optimize duplicated constants
            {
                HashSet<Field> hasMultipleWires = [];
                Dictionary<Field, Wire> constWireMapping = [];
                Dictionary<Wire, Wire> constWireReplaceMapping = [];
                foreach (Wire constWire in constantWires) {
                    Field value = constWire.ConstValue!;
                    if (constWireMapping.TryGetValue(value, out Wire? existingConstWire)) {
                        if (!hasMultipleWires.Contains(value)) {
                            Wire newConstWire = Wire.NewConstantWire(value, $"optimized_const_number_{value}");
                            constWireMapping[value] = newConstWire;
                            constWireReplaceMapping[existingConstWire] = newConstWire;
                            _ = hasMultipleWires.Add(value);

                            existingConstWire = newConstWire;
                        }

                        // replace constWire with existingConstWire
                        constWireReplaceMapping[constWire] = existingConstWire;
                        Trace.Assert(existingConstWire.Name == $"optimized_const_number_{value}");
                    }
                    else {
                        constWireMapping[value] = constWire;
                    }
                }

                // Modify each operations to replace wires
                foreach (Operation operation in operations) {
                    for (int i = 0; i < operation.InputWires.Count; i++) {
                        if (constWireReplaceMapping.TryGetValue(operation.InputWires[i], out Wire? value)) {
                            operation.InputWires[i] = value;
                        }
                    }
                    for (int i = 0; i < operation.OutputWires.Count; i++) {
                        if (constWireReplaceMapping.TryGetValue(operation.OutputWires[i], out Wire? value)) {
                            operation.OutputWires[i] = value;
                        }
                    }
                }

                // Replace constant wires
                constantWires = constWireMapping.Values.ToList();
            }

            // Re-order operations as well as their operation result wires in a topological order
            {
                List<Operation> orderedOperations = [];

                Dictionary<int, List<Operation>> operationsByLayer = [];
                {
                    void Add(Operation op) {
                        int layer = op.Layer;
                        if (!operationsByLayer.ContainsKey(layer)) {
                            operationsByLayer.Add(layer, []);
                        }
                        operationsByLayer[layer].Add(op);
                    }
                    operations.ForEach(Add);
                }

                // Print width and height
                {
                    int width = operationsByLayer.Values.Select(v => v.Count).Max();
                    double avgWidth = operationsByLayer.Values.Select(v => v.Count).Average();
                    int height = operationsByLayer.Keys.Max();
                    Serilog.Log.Debug($"CircuitBoard.Optimize(): Width={width} (avg {avgWidth:F2}); Height={height}");
                }

                orderedOperations = operationsByLayer.OrderBy(kv => kv.Key).SelectMany(kv => kv.Value).ToList();

                // Verify
                {
                    HashSet<Wire> knownWires = [.. constantWires, .. publicInputWires, .. privateInputWires];

                    for (int i = 0; i < orderedOperations.Count; i++) {
                        Operation currentOperation = orderedOperations[i];
                        Trace.Assert(currentOperation.InputWires.All(knownWires.Contains));
                        currentOperation.OutputWires.ForEach(wire => knownWires.Add(wire));
                    }
                }

                operations = orderedOperations;
            }

            // Return new circuitboard
            CircuitBoard newCircuitBoard = new();

            foreach (Wire wire in constantWires.Concat(privateInputWires).Concat(publicInputWires)) {
                newCircuitBoard.AddWire(wire);
            }

            foreach (Operation operation in operations) {
                newCircuitBoard.AddOperation(operation);
            }

            return newCircuitBoard;
        }

        List<Wire> constantWires = this.ConstantWires.ToList();
        List<Wire> publicInputWires = this.PublicInputWires.ToList();
        List<Wire> privateInputWires = this.PrivateInputWires.ToList();
        List<Operation> operations = this.Operations.ToList();
        return Func(constantWires, publicInputWires, privateInputWires, operations);
    }
}
