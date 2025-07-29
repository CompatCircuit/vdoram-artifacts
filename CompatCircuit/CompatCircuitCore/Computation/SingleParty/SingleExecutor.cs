using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.CompatCircuits;
using Anonymous.CompatCircuitCore.CompatCircuits.MpcCircuits;
using Anonymous.CompatCircuitCore.Computation.MultiParty;
using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.Computation.SingleParty;
/// <summary>
/// An executor computes a CompatCircuit in single node. Useful for single-user zero-knowledge proofs as well as debugging propose.
/// </summary>
public class SingleExecutor : IMpcExecutor {
    public string LoggerPrefix { get; init; } = "SingleExecutor";
    protected MpcExecutorState MpcExecutorState { get; private set; }
    public SingleExecutor() => this.MpcExecutorState = MpcExecutorState.InputRequired;

    public CircuitExecuteResult Compute(MpcCircuit mpcCircuit, IReadOnlyDictionary<int, Field> publicInputValueDict, IReadOnlyDictionary<int, Field> privateInputValueDict) {
        if (this.MpcExecutorState != MpcExecutorState.InputRequired) {
            throw new Exception("The computation can only be executed executed once");
        }
        this.MpcExecutorState = MpcExecutorState.Running;

        DateTimeOffset startTime = DateTimeOffset.Now;

        // Process PublicInputValues
        if (publicInputValueDict.Count != mpcCircuit.PublicInputWireCount - mpcCircuit.ConstantWireCount) {
            throw new Exception(
                $"The count of PublicInputValues is mismatched. Expected {mpcCircuit.PublicInputWireCount - mpcCircuit.ConstantWireCount}, got {publicInputValueDict.Count}."
            );
        }
        foreach ((int wireID, Field _) in publicInputValueDict) {
            if (wireID < mpcCircuit.ConstantWireCount || wireID >= mpcCircuit.PublicInputWireCount) {
                throw new Exception($"Wire ID {wireID} (public input) is out of range.");
            }
        }

        IReadOnlyList<Field> publicInputValues; // The values of public input wires. The i-th element in PublicInputValues corresponds to Wire i+ConstantWireCount.
        {
            List<Field> publicInputValueList = [];
            for (int i = mpcCircuit.ConstantWireCount; i < mpcCircuit.PublicInputWireCount; i++) {
                publicInputValueList.Add(publicInputValueDict[i]);
            }
            publicInputValues = publicInputValueList;
        }

        // Process PrivateInputValues
        if (privateInputValueDict.Count != mpcCircuit.InputWireCount - mpcCircuit.PublicInputWireCount) {
            throw new Exception(
                $"The count of PrivateInputValues is mismatched. Expected {mpcCircuit.InputWireCount - mpcCircuit.PublicInputWireCount}, got {privateInputValueDict.Count}."
            );
        }
        foreach ((int wireID, Field _) in privateInputValueDict) {
            if (wireID < mpcCircuit.PublicInputWireCount || wireID >= mpcCircuit.InputWireCount) {
                throw new Exception($"Wire ID {wireID} (private input) is out of range.");
            }
        }

        IReadOnlyList<Field> privateInputValues; // The values of private input wires. The i-th element in PrivateInputValues corresponds to Wire i+PublicInputWireCount.
        {
            List<Field> privateInputValueList = [];
            for (int i = mpcCircuit.PublicInputWireCount; i < mpcCircuit.InputWireCount; i++) {
                privateInputValueList.Add(privateInputValueDict[i]);
            }
            privateInputValues = privateInputValueList;
        }

        // Initialize ValueBoard
        List<Field?> valueBoard =
        [
            .. CompatCircuit.ReservedWireValues,
            .. mpcCircuit.ConstantInputs,
            .. publicInputValues,
            .. privateInputValues,
            .. Enumerable.Repeat<Field?>(null, mpcCircuit.WireCount - mpcCircuit.InputWireCount),
        ];
        Trace.Assert(valueBoard.Count == mpcCircuit.WireCount);

        Field? GetValueAt(int wireID) => valueBoard[wireID];
        void SetValueAt(int wireID, Field value) {
            Field? oldValue = valueBoard[wireID];
            if (oldValue is not null) {
                throw new Exception($"The value of wire {wireID} is already set with value {oldValue}.");
            }
            valueBoard[wireID] = value;
        }

        // Compute
        foreach (CompatCircuitOperation op in mpcCircuit.Operations) {
            // Fetch all values of input wires in order
            List<Field> inputValues = [];
            // Make sure all input wires have values
            foreach (int wireID in op.InputWires) {
                Field value = GetValueAt(wireID) ?? throw new Exception($"Wire {wireID} does not have a value, but the value is needed in operation {op}.");
                inputValues.Add(value);
            }

            // Skip wire count checking which is already ensured by CompatCircuit.Operation.CheckWireCount()

            // Compute based on operations
            switch (op.OperationType) {
                case CompatCircuitOperationType.Addition: {
                        int outputWireID = op.OutputWires[0];

                        Field result = ArithConfig.FieldFactory.Zero;
                        foreach (Field inputValue in inputValues) {
                            result += inputValue;
                        }

                        SetValueAt(outputWireID, result);
                    }
                    break;
                case CompatCircuitOperationType.Multiplication: {
                        int outputWireID = op.OutputWires[0];

                        Field result = ArithConfig.FieldFactory.One;
                        foreach (Field inputValue in inputValues) {
                            result *= inputValue;
                        }

                        SetValueAt(outputWireID, result);
                    }
                    break;
                case CompatCircuitOperationType.Inversion: {
                        int outputWireID = op.OutputWires[0];
                        Field input = inputValues[0];

                        Field result = input.InverseOrZero();

                        SetValueAt(outputWireID, result);
                    }
                    break;
                case CompatCircuitOperationType.BitDecomposition: {
                        IReadOnlyList<bool> bits = inputValues[0].BitDecomposition();
                        Trace.Assert(bits.Count == ArithConfig.BitSize);
                        for (int i = 0; i < ArithConfig.BitSize; i++) {
                            SetValueAt(op.OutputWires[i], ArithConfig.FieldFactory.New(bits[i]));
                        }
                    }
                    break;
                default:
                    throw new Exception($"Unknown operation type {op.OperationType}");
            }
        }

        this.MpcExecutorState = MpcExecutorState.Completed;

        DateTimeOffset endTime = DateTimeOffset.Now;
        return new CircuitExecuteResult() { MpcCircuit = mpcCircuit, ValueBoard = valueBoard.Select(v => new MpcValue(v, isSecretShare: false)).ToList(), TotalTime = endTime - startTime };
    }

    Task<CircuitExecuteResult> IMpcExecutor.Compute(MpcCircuit mpcCircuit, IReadOnlyDictionary<int, Field> publicInputValueDict, IReadOnlyDictionary<int, MpcValue> privateInputValueShareDict) {
        Dictionary<int, Field> privateInputValueDict = [];
        foreach ((int k, MpcValue v) in privateInputValueShareDict) {
            privateInputValueDict.Add(k, v.AssumeNonShare());
        }

        return Task.Run<CircuitExecuteResult>(() => this.Compute(mpcCircuit, publicInputValueDict, privateInputValueDict));
    }
}
