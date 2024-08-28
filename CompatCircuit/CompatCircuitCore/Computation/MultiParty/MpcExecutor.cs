using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.CompatCircuits;
using SadPencil.CompatCircuitCore.CompatCircuits.MpcCircuits;
using SadPencil.CompatCircuitCore.Computation.MultiParty.SharedStorages;
using SadPencil.CompatCircuitCore.Extensions;
using SadPencil.CompatCircuitCore.GlobalConfig;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.DaBitPrioPlus;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.EdaBitsKai;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.SecretSharing;
using SadPencil.CompatCircuitCore.SerilogHelpers;
using Serilog.Events;
using System.Diagnostics;
using System.Numerics;

namespace SadPencil.CompatCircuitCore.Computation.MultiParty;
public class MpcExecutor : IMpcExecutor {
    public string LoggerPrefix { get; init; } = "MPC";

    public MpcExecutorState MpcExecutorState { get; private set; }

    public MpcExecutorConfig MyConfig { get; }
    public int MyID => this.MyConfig.MyID;
    public int PartyCount => this.MyConfig.PartyCount;

    private IMpcSharedStorage MpcSharedStorage { get; }

    private IEnumerator<FieldBeaverTripleShare> MyFieldBeaverTripleShares { get; }
    private IEnumerator<BoolBeaverTripleShare> MyBoolBeaverTripleShares { get; }
    private IEnumerator<EdaBitsKaiShare> MyEdaBitsShares { get; }
    private IEnumerator<DaBitPrioPlusShare> MyDaBitPrioPlusShares { get; }

    private List<MpcValue?>? valueBoard = null;

    private static IReadOnlyList<bool> FieldSizeMinusTwoBits { get; } = (ArithConfig.FieldFactory.NegOne - ArithConfig.FieldFactory.One).BitDecomposition();

    public MpcExecutor(
        MpcExecutorConfig myMpcExecutorConfig,
        IMpcSharedStorage mpcSharedStorage,
        IEnumerator<FieldBeaverTripleShare> myFieldBeaverTripleShareEnumerator,
        IEnumerator<BoolBeaverTripleShare> myBoolBeaverTripleShareEnumerator,
        IEnumerator<EdaBitsKaiShare> myEdaBitsShareEnumerator,
        IEnumerator<DaBitPrioPlusShare> myDaBitPrioPlusShareEnumerator) {

        this.MyConfig = myMpcExecutorConfig;

        this.MpcSharedStorage = mpcSharedStorage;

        this.MyFieldBeaverTripleShares = myFieldBeaverTripleShareEnumerator;
        this.MyBoolBeaverTripleShares = myBoolBeaverTripleShareEnumerator;
        this.MyEdaBitsShares = myEdaBitsShareEnumerator;
        this.MyDaBitPrioPlusShares = myDaBitPrioPlusShareEnumerator;

        this.MpcExecutorState = MpcExecutorState.InputRequired;
    }

    public MpcExecutor(
        MpcExecutorConfig myMpcExecutorConfig,
        IMpcSharedStorage mpcSharedStorage,
        FieldBeaverTripleShareList myFieldBeaverTripleShareList,
        BoolBeaverTripleShareList myBoolBeaverTripleShareList,
        EdaBitsKaiShareList myEdaBitsShareList,
        DaBitPrioPlusShareList myDaBitPrioPlusShareList) : this(
            myMpcExecutorConfig,
            mpcSharedStorage,
            myFieldBeaverTripleShareList.GetEnumerator(),
            myBoolBeaverTripleShareList.GetEnumerator(),
            myEdaBitsShareList.GetEnumerator(),
            myDaBitPrioPlusShareList.GetEnumerator()) { }

    private async Task Until(Func<bool> until) => await AsyncHelper.TerminateOnException(async () => {
        if (this.MyConfig.TickMS == 0) {
            int bruteRetryCount = 50000000;
            for (int retryTime = 0; retryTime < bruteRetryCount; retryTime++) {
                if (until()) {
                    return;
                }
            }

            Serilog.Log.Warning($"[{this.LoggerPrefix}] The wait has been too long.");
        }

        bool verbose = false;
        LogEventLevel oldLogEventLevel = SerilogHelper.LoggingLevelSwitch.MinimumLevel;

        void Verbose() {
            if (verbose) {
                return;
            }

            SerilogHelper.LoggingLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            Serilog.Log.Verbose("Well, let's see if this helps.");
            verbose = true;
        }

        void UnVerbose() {
            if (!verbose) {
                return;
            }

            SerilogHelper.LoggingLevelSwitch.MinimumLevel = oldLogEventLevel;
            verbose = false;
        }

        int tickMS = this.MyConfig.TickMS == 0 ? 1 : this.MyConfig.TickMS;
        int terminateCount = 20;
        for (int terminateRetryIndex = 0; terminateRetryIndex < terminateCount; terminateRetryIndex++) {
            int retryCount = (1000 / tickMS) + 1;
            for (int retryTime = 0; retryTime < retryCount; retryTime++) {
                if (until()) {
                    UnVerbose();
                    return;
                }

                await Task.Delay(tickMS);
            }

            Serilog.Log.Warning($"[{this.LoggerPrefix}] The wait has been too long.");

            Verbose();
        }

        throw new Exception("Unknow concurrency or networking issue occurs. Computation aborts. Please re-try.");
    });

    private MpcValue? GetValueAt(int wireID) => this.valueBoard is null
            ? throw new Exception("ValueBoard is not initialized. This method can only be called during/after Compute() method.")
            : this.valueBoard[wireID];

    private void SetValueAt(int wireID, MpcValue value, bool overrideExisting = false) {
        if (this.valueBoard is null) {
            throw new Exception("ValueBoard is not initialized. This method can only be called during/after Compute() method.");
        }

        MpcValue? oldValue = this.valueBoard[wireID];

        // Set value
        if (oldValue is not null && !overrideExisting) {
            throw new Exception($"The value of wire {wireID} is already set with value {oldValue}.");
        }
        this.valueBoard[wireID] = value;
    }

    private Field GetSecretShare(MpcValue value) => value.IsSecretShare ? value.Value : this.MyID == 0 ? value.Value : ArithConfig.FieldFactory.Zero;
    private IReadOnlyList<int> AllPartiesButMe => Enumerable.Range(0, this.PartyCount).Where(x => x != this.MyID).ToList();

    private async Task ComputeOperations(MpcCircuit mpcCircuit) => await AsyncHelper.TerminateOnException(async () => {
        // This method modifies this.valueBoard

        // Compute each operation one by one
        // TODO: optimize this by parallelizing the computation (wires must be layered)

        DateTimeOffset lastReportTime = DateTimeOffset.Now;
        //bool lastOperationIsInvOrBitDecomposition = false;
        for (int opID = 0; opID < mpcCircuit.Operations.Count; opID++) {
            CompatCircuitOperation op = mpcCircuit.Operations[opID];

            {
                string percentage = ((double)opID / mpcCircuit.Operations.Count * 100).ToString("F2") + "%";
                string message = $"[{this.LoggerPrefix}] [{percentage}] [{opID}] Process operation {op}.";
                Serilog.Log.Debug(message);

                //bool thisOperationIsInvOrBitDecomposition = op.OperationType is CompatCircuitOperationType.BitDecomposition or CompatCircuitOperationType.Inversion;
                DateTimeOffset currentTime = DateTimeOffset.Now;

                if (opID == 0 || opID == mpcCircuit.Operations.Count - 1 ||
                    //thisOperationIsInvOrBitDecomposition ||
                    //lastOperationIsInvOrBitDecomposition ||
                    (currentTime - lastReportTime).TotalMilliseconds > 1000) {

                    Serilog.Log.Information(message);
                    lastReportTime = currentTime;
                }

                //lastOperationIsInvOrBitDecomposition = thisOperationIsInvOrBitDecomposition;
            }

            // Fetch all values of input wires in order
            List<MpcValue> inputValueTags = [];
            // Make sure all input wires have values
            foreach (int wireID in op.InputWires) {
                MpcValue valueTag = this.GetValueAt(wireID) ?? throw new Exception($"Wire {wireID} does not have a value, but the value is needed in operation {op}.");
                inputValueTags.Add(valueTag);
            }

            bool containNoSecretShares = inputValueTags.All(tag => !tag.IsSecretShare);

            // Skip wire count checking which is already ensured by CompatCircuit.Operation.CheckWireCount()

            // Compute based on operations
            if (op.OperationType == CompatCircuitOperationType.BitDecomposition) {
                if (containNoSecretShares) {
                    Trace.Assert(!inputValueTags[0].IsSecretShare);
                    IReadOnlyList<bool> bits = inputValueTags[0].Value.BitDecomposition();
                    Trace.Assert(bits.Count == ArithConfig.BitSize);
                    for (int i = 0; i < ArithConfig.BitSize; i++) {
                        this.SetValueAt(op.OutputWires[i], new MpcValue() { IsSecretShare = false, Value = ArithConfig.FieldFactory.New(bits[i]) });
                    }
                }
                else {
                    Trace.Assert(inputValueTags[0].IsSecretShare);

                    // The following steps are *modified* from the following paper. It comes with optimizations and DOES NOT follow the paper exactly.

                    // Damgård, Ivan, et al. "Unconditionally secure constant-rounds multi-party computation for equality, comparison, bits and exponentiation." Theory of Cryptography Conference. Berlin, Heidelberg: Springer Berlin Heidelberg, 2006.
                    // https://link.springer.com/content/pdf/10.1007/11681878_15.pdf

                    // Each party has a secret share of the secret value
                    Field aShare = inputValueTags[0].Value;

                    // m = 2 ^ BitSize, no less than p

                    // Fetch m bits
                    Field bShare;
                    List<bool> bBitsShares;
                    {
                        bool hasValue = this.MyEdaBitsShares.MoveNext();
                        if (!hasValue) {
                            throw new Exception("Insufficient pre-shared edaBits shares");
                        }
                        EdaBitsKaiShare edaBits = this.MyEdaBitsShares.Current;
                        bBitsShares = edaBits.BoolShares.ToList();
                        bShare = edaBits.ArithShare;
                    }

                    // compute [c] = [a] - [b] in field p
                    Field cShare = aShare - bShare;

                    // reveal c
                    Field cRecovered = await this.ExposeFieldValue(cShare, $"{op.OutputWires[0]}_c");

                    // e = c + 2^BitSize - p
                    Ring eValue = ArithConfig.ExtRingFactory.New(cRecovered.Value + ArithConfig.BaseRingFactory.RingSize - ArithConfig.FieldSize);

                    // Use bit adder to compute d' = b + e
                    bBitsShares.Add(false); // Add a dummy 0 bit
                    Trace.Assert(bBitsShares.Count == ArithConfig.BitSize + 1);
                    List<bool> dPrimeBitsShare = await this.BitsAddConst(bBitsShares, eValue.BitDecomposition().ToList(), $"{opID}_dPrime");

                    // Fetch q = bool(d >= p), where d = b + c. The correctness of q is covered by unit test
                    // Note: in the paper, q = bool(d > p). I think it should be d >= p.
                    bool qShare = dPrimeBitsShare[ArithConfig.BitSize];

                    // Get NOT q
                    bool notQShare = this.BitwiseNot(qShare);

                    // hBits = dPrimeBitsShare + notQ * p
                    // prepare pBits
                    List<bool> pBits = ArithConfig.BaseRingFactory.New(ArithConfig.FieldSize).BitDecomposition().ToList();
                    // multiply pBits with notQ
                    List<bool> optionalPBits = pBits.Select((bit, i) => bit & notQShare).ToList();
                    // compute hBits = dPrimeBitsShare + optionalPBits
                    dPrimeBitsShare.RemoveAt(ArithConfig.BitSize);
                    Trace.Assert(dPrimeBitsShare.Count == ArithConfig.BitSize);
                    Trace.Assert(optionalPBits.Count == ArithConfig.BitSize);
                    List<bool> hBits = await this.BitsAdd(dPrimeBitsShare, optionalPBits, $"{opID}_h");

                    // The correctness of final result is covered by unit test

                    // Save the result
                    for (int i = 0; i < ArithConfig.BitSize; i++) {
                        // TODO: parallelize this for loop

                        int outputWireID = op.OutputWires[i];
                        // Expose if the output value is marked as public
                        if (mpcCircuit.PublicOutputs.Contains(outputWireID)) {
                            bool recovered = await this.ExposeBoolValue(hBits[i], $"output_{outputWireID}");
                            this.SetValueAt(outputWireID, new MpcValue() { IsSecretShare = false, Value = ArithConfig.FieldFactory.New(recovered) }, overrideExisting: true);
                            Serilog.Log.Debug($"[{this.LoggerPrefix}] Public output wire {outputWireID}: {(recovered ? 1 : 0)}");
                        }
                        else {
                            // 6. Convert from boolean share to field share
                            // TODO: parallelize this for loop

                            // Fetch a daBitPrioPlus pair
                            bool hasValue = this.MyDaBitPrioPlusShares.MoveNext();
                            if (!hasValue) {
                                throw new Exception("Insufficient pre-shared daBitPrioPlus shares");
                            }
                            DaBitPrioPlusShare daBitPrioPlusShare = this.MyDaBitPrioPlusShares.Current;

                            // Compute [d] = [b]_B ^ [r]_B
                            bool deltaShare = daBitPrioPlusShare.BoolShare ^ hBits[i];

                            // Recover [d]
                            bool delta = await this.ExposeBoolValue(deltaShare, $"{op.OutputWires[i]}_delta");

                            // If [d] is false, use [r]_A, otherwise compute 1 - [r]_A
                            Field finalBitShare;
                            if (!delta) {
                                finalBitShare = daBitPrioPlusShare.ArithShare;
                            }
                            else {
                                Field constNumberOneShare = this.GetSecretShare(new MpcValue(ArithConfig.FieldFactory.One, isSecretShare: false));
                                finalBitShare = constNumberOneShare - daBitPrioPlusShare.ArithShare;
                            }

                            this.SetValueAt(op.OutputWires[i], new MpcValue() { IsSecretShare = true, Value = finalBitShare });
                        }
                    }

                    // That's all
                }
            }
            else {
                switch (op.OperationType) {
                    case CompatCircuitOperationType.Addition: {
                            int outputWireID = op.OutputWires[0];

                            if (containNoSecretShares) {
                                Field result = ArithConfig.FieldFactory.Zero;
                                foreach (MpcValue inputValueTag in inputValueTags) {
                                    result += inputValueTag.Value;
                                }
                                this.SetValueAt(outputWireID, new MpcValue() { IsSecretShare = false, Value = result });
                            }
                            else {
                                Field resultShare = ArithConfig.FieldFactory.Zero;
                                foreach (MpcValue inputValueTag in inputValueTags) {
                                    resultShare += this.GetSecretShare(inputValueTag);
                                }
                                this.SetValueAt(outputWireID, new MpcValue() { IsSecretShare = true, Value = resultShare });
                            }
                        }
                        break;
                    case CompatCircuitOperationType.Multiplication: {
                            int outputWireID = op.OutputWires[0];

                            if (containNoSecretShares) {
                                Field result = ArithConfig.FieldFactory.One;
                                foreach (MpcValue inputValueTag in inputValueTags) {
                                    result *= inputValueTag.Value;
                                }
                                this.SetValueAt(outputWireID, new MpcValue() { IsSecretShare = false, Value = result });
                            }
                            else {
                                Field left = this.GetSecretShare(inputValueTags[0]);
                                for (int i = 1; i < inputValueTags.Count; i++) {
                                    Field right = this.GetSecretShare(inputValueTags[i]);
                                    left = await this.BeaverMulti(left, right, $"{opID}_multi_{i}");
                                }

                                this.SetValueAt(outputWireID, new MpcValue() { IsSecretShare = true, Value = left });
                            }
                        }
                        break;
                    case CompatCircuitOperationType.Inversion: {
                            int outputWireID = op.OutputWires[0];

                            if (containNoSecretShares) {
                                Field input = inputValueTags[0].Value;
                                Field result = input.InverseOrZero();
                                this.SetValueAt(outputWireID, new MpcValue() { IsSecretShare = false, Value = result });
                            }
                            else {
                                Field baseShare = this.GetSecretShare(inputValueTags[0]);
                                Field? result = null;
                                Field baseToCurrentPower = baseShare;

                                for (int i = 0; i < FieldSizeMinusTwoBits.Count; i++) {
                                    if (FieldSizeMinusTwoBits[i]) {
                                        result = result is null ? baseToCurrentPower : await this.BeaverMulti(result, baseToCurrentPower, $"{opID}_inv_{i}_1");
                                    }
                                    baseToCurrentPower = await this.BeaverMulti(baseToCurrentPower, baseToCurrentPower, $"{opID}_inv_{i}_2");
                                }

                                Trace.Assert(result is not null); // Since p - 2 is not zero
                                this.SetValueAt(outputWireID, new MpcValue() { IsSecretShare = true, Value = result });
                            }
                        }
                        break;
                    default:
                        throw new Exception($"Unknown operation type {op.OperationType}");
                }

                // Expose if the output value is marked as public
                {
                    Trace.Assert(op.OutputWires.Count == 1);
                    int outputWireID = op.OutputWires[0];
                    if (mpcCircuit.PublicOutputs.Contains(outputWireID)) {
                        MpcValue valueTag = this.GetValueAt(outputWireID) ?? throw new Exception($"Assert failed. Wire {outputWireID} should already be saved. This should not happen.");
                        if (!valueTag.IsSecretShare) {
                            continue;
                        }

                        Field recovered = await this.ExposeFieldValue(valueTag.Value, $"output_{outputWireID}");
                        this.SetValueAt(outputWireID, new MpcValue() { IsSecretShare = false, Value = recovered }, overrideExisting: true);
                        Serilog.Log.Debug($"[{this.LoggerPrefix}] Public output wire {outputWireID}: {recovered}");
                    }
                }
            }
        }
    });

    /// <summary>
    /// Compute the MPC circuit.
    /// </summary>
    /// <param name="mpcCircuit">The MPC circuit to be computed.</param>
    /// <param name="publicInputValueDict">The values of public input wires, whose ID is between [ConstantWireCount, PublicInputWireCount).</param>
    /// <param name="privateInputValueShareDict">The secret shares of private input wires, whose ID is between [PublicInputWireCount, InputWireCount).<br/>
    /// <returns>All wire values or their secret shares. It is guaranteed that values of public output wires are not secret shares.</returns>
    public async Task<CircuitExecuteResult> Compute(MpcCircuit mpcCircuit, IReadOnlyDictionary<int, Field> publicInputValueDict, IReadOnlyDictionary<int, MpcValue> privateInputValueShareDict) => await AsyncHelper.TerminateOnException(async () => {
        if (this.MpcExecutorState != MpcExecutorState.InputRequired) {
            throw new Exception("The computation can only be executed executed once");
        }
        Trace.Assert(this.valueBoard is null);

        this.MpcExecutorState = MpcExecutorState.Running;

        DateTimeOffset startTime = DateTimeOffset.Now;

        // Check inputs
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

        if (privateInputValueShareDict.Count != mpcCircuit.InputWireCount - mpcCircuit.PublicInputWireCount) {
            throw new Exception(
                $"The count of PrivateInputValues is mismatched. Expected {mpcCircuit.InputWireCount - mpcCircuit.PublicInputWireCount}, got {privateInputValueShareDict.Count}."
            );
        }

        foreach ((int wireID, MpcValue _) in privateInputValueShareDict) {
            if (wireID < mpcCircuit.PublicInputWireCount || wireID >= mpcCircuit.InputWireCount) {
                throw new Exception($"Wire ID {wireID} (private input) is out of range.");
            }
        }

        // Convert public input values to a list        
        IReadOnlyList<Field> publicInputValues; // The values of public input wires. The wireID-th element in PublicInputValues corresponds to Wire wireID+ConstantWireCount.
        {
            List<Field> _publicInputValues = [];
            for (int i = mpcCircuit.ConstantWireCount; i < mpcCircuit.PublicInputWireCount; i++) {
                _publicInputValues.Add(publicInputValueDict[i]);
            }
            publicInputValues = _publicInputValues;
        }

        // Initialize valueBoard
        {
            List<MpcValue?> valueBoard = [];
            this.valueBoard = [];

            List<Field> knownValues = [
                .. CompatCircuit.ReservedWireValues,
                .. mpcCircuit.ConstantInputs,
                .. publicInputValues,
            ];

            foreach (Field field in knownValues) {
                valueBoard.Add(new MpcValue() { IsSecretShare = false, Value = field });
            }

            // Add private input values
            for (int wireID = mpcCircuit.PublicInputWireCount; wireID < mpcCircuit.InputWireCount; wireID++) {
                MpcValue value = privateInputValueShareDict[wireID];
                if (!value.IsSecretShare) {
                    throw new Exception($"Wire ID {wireID} (private input) is not a secret share.");
                }
                valueBoard.Add(value);
            }

            valueBoard.AddRange(Enumerable.Repeat<MpcValue?>(null, mpcCircuit.WireCount - mpcCircuit.InputWireCount));

            Trace.Assert(valueBoard.Count == mpcCircuit.WireCount);
            this.valueBoard = valueBoard;
        }

        // Send online message, while waiting for other nodes being ready
        Serilog.Log.Information($"[{this.LoggerPrefix}] Send online message, while waiting for other nodes being ready.");
        this.MpcSharedStorage.SetPartyOnline(this.MyID);
        await this.Until(until: () => this.MpcSharedStorage.GetPartyOnlineAllParties().All(online => online));

        // Compute the circuit
        await this.ComputeOperations(mpcCircuit);

        // Wait for all parties ends
        {
            Serilog.Log.Information($"[{this.LoggerPrefix}] Completed. Wait for other parties...");
            this.MpcSharedStorage.SetPartyCompleted(this.MyID);
            await this.Until(until: () => this.MpcSharedStorage.GetPartyCompletedAllParties().All(completed => completed));
        }

        Serilog.Log.Information($"[{this.LoggerPrefix}] All parties completed.");

        // All public outputs should be exposed. Skip this check as it will be handled by CircuitExecuteResult.

        // Mark as completed
        this.MpcExecutorState = MpcExecutorState.Completed;

        DateTimeOffset endTime = DateTimeOffset.Now;
        // Return the result
        return new CircuitExecuteResult() { MpcCircuit = mpcCircuit, ValueBoard = this.valueBoard, TotalTime = endTime - startTime };
    });

    /// <summary>
    /// Compute the MPC circuit.
    /// </summary>
    /// <param name="mpcCircuit">The MPC circuit to be computed.</param>
    /// <param name="publicInputValueDict">The values of public input wires, whose ID is between [ConstantWireCount, PublicInputWireCount).</param>
    /// <param name="privateInputValueDict">The plaintext values of private input wires, whose ID is between [PublicInputWireCount, InputWireCount).<br/>
    /// Private input values not owned by this party will be secret shared from other parties, so they must not be included in this dictionary.<br/>
    /// Warning: each private input wire can only be owned by one party! Will not check for this!</param>
    /// <returns>All wire values or their secret shares. It is guaranteed that values of public output wires are not secret shares.</returns>
    public async Task<CircuitExecuteResult> Compute(MpcCircuit mpcCircuit, IReadOnlyDictionary<int, Field> publicInputValueDict, IReadOnlyDictionary<int, Field> privateInputValueDict) => await AsyncHelper.TerminateOnException(async () => {
        if (this.MpcExecutorState != MpcExecutorState.InputRequired) {
            throw new Exception("The computation can only be executed executed once");
        }
        Trace.Assert(this.valueBoard is null);

        this.MpcExecutorState = MpcExecutorState.Running;

        DateTimeOffset startTime = DateTimeOffset.Now;

        // Check inputs
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

        foreach ((int wireID, Field _) in privateInputValueDict) {
            if (wireID < mpcCircuit.PublicInputWireCount || wireID >= mpcCircuit.InputWireCount) {
                throw new Exception($"Wire ID {wireID} (private input) is out of range.");
            }
        }

        // Convert public input values to a list        
        IReadOnlyList<Field> publicInputValues; // The values of public input wires. The wireID-th element in PublicInputValues corresponds to Wire wireID+ConstantWireCount.
        {
            List<Field> _publicInputValues = [];
            for (int i = mpcCircuit.ConstantWireCount; i < mpcCircuit.PublicInputWireCount; i++) {
                _publicInputValues.Add(publicInputValueDict[i]);
            }
            publicInputValues = _publicInputValues;
        }

        // Initialize valueBoard
        {
            List<MpcValue?> valueBoard = [];
            this.valueBoard = [];

            List<Field> knownValues = [
                .. CompatCircuit.ReservedWireValues,
                .. mpcCircuit.ConstantInputs,
                .. publicInputValues,
            ];

            foreach (Field field in knownValues) {
                valueBoard.Add(new MpcValue() { IsSecretShare = false, Value = field });
            }

            valueBoard.AddRange(Enumerable.Repeat<MpcValue?>(null, mpcCircuit.WireCount - mpcCircuit.PublicInputWireCount));

            Trace.Assert(valueBoard.Count == mpcCircuit.WireCount);
            this.valueBoard = valueBoard;
        }

        // Send online message, while waiting for other nodes being ready
        Serilog.Log.Information($"[{this.LoggerPrefix}] Send online message, while waiting for other nodes being ready.");
        this.MpcSharedStorage.SetPartyOnline(this.MyID);
        await this.Until(until: () => this.MpcSharedStorage.GetPartyOnlineAllParties().All(online => online));

        // Make shares for my private inputs
        // Warning: each private input value can only be owned by one party! Will not check for this!
        // TODO: implement a protocol for checking this
        IReadOnlyDictionary<int, List<Field>> privateInputSharesForAllParties;
        {
            Dictionary<int, List<Field>> privateInputSharesForAllPartieDict = [];
            foreach ((int wireID, Field value) in privateInputValueDict) {
                List<Field> shares = ArithConfig.FieldSecretSharing.MakeShares(this.PartyCount, value);
                privateInputSharesForAllPartieDict.Add(wireID, shares);

                this.valueBoard[wireID] = new() {
                    Value = shares[this.MyID],
                    IsSecretShare = true,
                };
            }

            privateInputSharesForAllParties = privateInputSharesForAllPartieDict;
        }

        // Send my private input values to other parties, while waiting for remaining private input values.
        Serilog.Log.Information($"[{this.LoggerPrefix}] Send my private input values to other parties, while waiting for remaining private input values.");
        foreach ((int wireID, List<Field> shares) in privateInputSharesForAllParties) {
            foreach (int receiverID in this.AllPartiesButMe) {
                this.MpcSharedStorage.SetInputShareVector($"{wireID}", senderPartyIndex: this.MyID, receiverID, [shares[receiverID].Value]);
            }
        }

        for (int wireID = mpcCircuit.PublicInputWireCount; wireID < mpcCircuit.InputWireCount; wireID++) {
            if (this.valueBoard[wireID] is not null) {
                continue;
            }

            await this.Until(() => {
                Trace.Assert(this.valueBoard[wireID] is null, $"Wire {wireID} should be null");

                IReadOnlyList<BigInteger>? shares = this.MpcSharedStorage.GetInputShareVector($"{wireID}", this.MyID);
                if (shares is null) {
                    return false;
                }

                Trace.Assert(shares.Count == 1);
                Field share = ArithConfig.FieldFactory.New(shares[0]);

                this.valueBoard[wireID] = new MpcValue() { IsSecretShare = true, Value = share };
                Serilog.Log.Debug($"[{this.LoggerPrefix}] Received share of wire ID {wireID}");

                return true;
            });
        }

        // Compute the circuit
        await this.ComputeOperations(mpcCircuit);

        DateTimeOffset endTime = DateTimeOffset.Now;
        TimeSpan totalTime = endTime - startTime;

        // Wait for all parties ends
        {
            Serilog.Log.Information($"[{this.LoggerPrefix}] Completed. Time cost: {totalTime.TotalSeconds:F6} sec. Wait for other parties...");
            this.MpcSharedStorage.SetPartyCompleted(this.MyID);
            await this.Until(until: () => this.MpcSharedStorage.GetPartyCompletedAllParties().All(completed => completed));
        }

        // All public outputs should be exposed. Skip this check as it will be handled by CircuitExecuteResult.

        // Mark as completed
        this.MpcExecutorState = MpcExecutorState.Completed;

        // Return the result
        return new CircuitExecuteResult() { MpcCircuit = mpcCircuit, ValueBoard = this.valueBoard, TotalTime = totalTime };
    });

    private async Task<Ring> ExposeRingValue(Ring myShare, string key, RingFactory ringFactory, RingSecretSharing ringSecretSharing) => (await this.ExposeRingValues([myShare], key, ringFactory, ringSecretSharing))[0];

    private async Task<List<Ring>> ExposeRingValues(IReadOnlyList<Ring> myShare, string key, RingFactory ringFactory, RingSecretSharing ringSecretSharing) => await AsyncHelper.TerminateOnException(async () => {
        List<Ring>? returnValue = null;
        List<BigInteger> valuesToBeSend = myShare.Select(v => v.Value).ToList();

        Serilog.Log.Debug($"[{this.LoggerPrefix}] ExposeRingValues. Key: {key}");
        this.MpcSharedStorage.SetExposedBigIntegerShareVector(key, senderPartyIndex: this.MyID, valuesToBeSend);
        await this.Until(() => {
            IReadOnlyList<IReadOnlyList<BigInteger>?> sharesForParties = this.MpcSharedStorage.GetExposedBigIntegerShareVectorAllParties(key);
            List<int> remainingParties = [];
            for (int i = 0; i < this.PartyCount; i++) {
                if (sharesForParties[i] is null) {
                    remainingParties.Add(i);
                }
            }

            if (remainingParties.Count > 0) {
                return false;
            }

            returnValue = [];
            for (int i = 0; i < myShare.Count; i++) {
                List<Ring> shares = [];
                for (int partyIndex = 0; partyIndex < this.PartyCount; partyIndex++) {
                    shares.Add(ringFactory.New(sharesForParties[partyIndex]![i]));
                }

                returnValue.Add(ringSecretSharing.RecoverFromShares(this.PartyCount, shares));
            }

            return true;

        });

        Trace.Assert(returnValue is not null && returnValue.Count == myShare.Count);
        return returnValue;
    });

    private async Task<Field> ExposeFieldValue(Field myShare, string key) => (await this.ExposeFieldValues([myShare], key))[0];

    private async Task<List<Field>> ExposeFieldValues(IReadOnlyList<Field> myShare, string key) => await AsyncHelper.TerminateOnException(async () => {
        Serilog.Log.Debug($"[{this.LoggerPrefix}] ExposeFieldValues. Key: {key}");
        List<Field>? returnValue = null;
        List<BigInteger> valuesToBeSend = myShare.Select(v => v.Value).ToList();

        Serilog.Log.Debug($"[{this.LoggerPrefix}] ExposeFieldValues. Key: {key}");
        this.MpcSharedStorage.SetExposedBigIntegerShareVector(key, senderPartyIndex: this.MyID, valuesToBeSend);
        await this.Until(() => {
            IReadOnlyList<IReadOnlyList<BigInteger>?>? sharesForParties = this.MpcSharedStorage.GetExposedBigIntegerShareVectorAllParties(key);
            if (sharesForParties is null) {
                return false;
            }

            List<int> remainingParties = [];
            for (int i = 0; i < this.PartyCount; i++) {
                if (sharesForParties[i] is null) {
                    remainingParties.Add(i);
                }
            }
            if (remainingParties.Count > 0) {
                return false;
            }

            returnValue = [];
            for (int i = 0; i < myShare.Count; i++) {
                List<Field> shares = [];
                for (int partyIndex = 0; partyIndex < this.PartyCount; partyIndex++) {
                    shares.Add(ArithConfig.FieldFactory.New(sharesForParties[partyIndex]![i]));
                }

                returnValue.Add(ArithConfig.FieldSecretSharing.RecoverFromShares(this.PartyCount, shares));
            }

            return true;
        });

        Trace.Assert(returnValue is not null && returnValue.Count == myShare.Count);
        return returnValue;
    });

    private async Task<Field> BeaverMulti(Field leftShare, Field rightShare, string keyPrefix) => await AsyncHelper.TerminateOnException(async () => {
        // Fetch an unused beaver triple
        bool hasValue = this.MyFieldBeaverTripleShares.MoveNext();
        if (!hasValue) {
            throw new Exception("Insufficient pre-shared field beaver triple shares");
        }
        FieldBeaverTripleShare triple = this.MyFieldBeaverTripleShares.Current;

        // Compute dA = [a] - [x]; dB = [b] - [y] (note: a: left, b: right)
        Field dAShare = leftShare - triple.X;
        Field dBShare = rightShare - triple.Y;

        List<Field> recovered = await this.ExposeFieldValues([dAShare, dBShare], keyPrefix);
        Field dARecovered = recovered[0];
        Field dBRecovered = recovered[1];

        // [a * b] = [XY] + dA * [Y] + dB * [X] + dA * dB (note: since it's additive secret sharing, we use dA * [dB] to replace dA * dB)
        Field productShare = triple.XY + (dARecovered * triple.Y) + (dBRecovered * triple.X) + (dARecovered * dBShare);
        return productShare;
    });

    private async Task<bool> BeaverBitwiseAnd(bool leftShare, bool rightShare, string keyPrefix) => await AsyncHelper.TerminateOnException(async () => {
        // Fetch an unused beaver triple
        bool hasValue = this.MyBoolBeaverTripleShares.MoveNext();
        if (!hasValue) {
            throw new Exception("Insufficient pre-shared boolean beaver triple shares");
        }
        BoolBeaverTripleShare triple = this.MyBoolBeaverTripleShares.Current;

        // Compute dA = [a] ^ [x]; dB = [b] ^ [y] (note: a: left, b: right)
        bool dAShare = leftShare ^ triple.X;
        bool dBShare = rightShare ^ triple.Y;

        List<bool> recovered = await this.ExposeBoolValues([dAShare, dBShare], keyPrefix);
        bool dARecovered = recovered[0];
        bool dBRecovered = recovered[1];

        // [a & b] = [XY] ^ dA & [Y] ^ dB & [X] ^ dA & dB (note: since it's additive secret sharing, we use dA & [dB] to replace dA & dB)
        bool productShare = triple.XY ^ (dARecovered & triple.Y) ^ (dBRecovered & triple.X) ^ (dARecovered & dBShare);
        return productShare;
    });

    private async Task<bool> ExposeBoolValue(bool myShare, string key) => (await this.ExposeBoolValues([myShare], key))[0];
    private async Task<List<bool>> ExposeBoolValues(IReadOnlyList<bool> myBitShares, string key) => await AsyncHelper.TerminateOnException(async () => {
        List<bool>? returnValue = null;

        Serilog.Log.Debug($"[{this.LoggerPrefix}] ExposeBoolValues. Key: {key}");
        this.MpcSharedStorage.SetExposedBoolShareVector(key, senderPartyIndex: this.MyID, myBitShares);
        await this.Until(() => {
            IReadOnlyList<IReadOnlyList<bool>?>? sharesForParties = this.MpcSharedStorage.GetExposedBoolShareVectorAllParties(key);
            if (sharesForParties is null) {
                return false;
            }
            List<int> remainingParties = [];
            for (int i = 0; i < this.PartyCount; i++) {
                if (sharesForParties[i] is null) {
                    remainingParties.Add(i);
                }
            }
            if (remainingParties.Count > 0) {
                return false;
            }

            returnValue = [];
            for (int i = 0; i < myBitShares.Count; i++) {
                List<bool> shares = [];
                for (int partyIndex = 0; partyIndex < this.PartyCount; partyIndex++) {
                    shares.Add(sharesForParties[partyIndex]![i]);
                }

                returnValue.Add(ArithConfig.BoolSecretSharing.RecoverFromShares(this.PartyCount, shares));
            }

            return true;
        });

        Trace.Assert(returnValue is not null && returnValue.Count == myBitShares.Count);
        return returnValue;
    });

    private async Task<bool> BeaverBitwiseOr(bool leftShare, bool rightShare, string keyPrefix) => leftShare ^ rightShare ^ await this.BeaverBitwiseAnd(leftShare, rightShare, keyPrefix);

    private bool BitwiseNot(bool share) => this.MyID == 0 ? !share : share;
    private bool BitwiseXor(bool leftShare, bool rightShare) => leftShare ^ rightShare;

    private async Task<List<bool>> BitsAddConst(IReadOnlyList<bool> leftBitsShare, IReadOnlyList<bool> rightBitsConst, string keyPrefix, bool preserveOverflow = false) => await AsyncHelper.TerminateOnException(async () => {
        if (leftBitsShare.Count != rightBitsConst.Count) {
            throw new Exception("leftBitsShare.Count != rightBitsConst.Count");
        }

        int bitCount = leftBitsShare.Count;
        List<bool> resultBitsShare = [];
        bool carryShare = false;

        for (int i = 0; i < bitCount; i++) {
            bool aBitShare = leftBitsShare[i];
            // Convert public bit into secret shares
            bool bBit = this.MyID == 0 && rightBitsConst[i];

            resultBitsShare.Add(aBitShare ^ bBit ^ carryShare);

            // Update carryShare
            if (!rightBitsConst[i]) {
                bool t2 = await this.BeaverBitwiseAnd(aBitShare, carryShare, $"{keyPrefix}_{i}_2");
                carryShare = t2;
            }
            else {
                bool t1 = aBitShare;
                bool t2 = await this.BeaverBitwiseAnd(aBitShare, carryShare, $"{keyPrefix}_{i}_2");
                bool t3 = carryShare;
                bool t4 = await this.BeaverBitwiseOr(t1, t2, $"{keyPrefix}_{i}_4");
                bool t5 = await this.BeaverBitwiseOr(t4, t3, $"{keyPrefix}_{i}_5");
                carryShare = t5;
            }
        }

        if (preserveOverflow) {
            resultBitsShare.Add(carryShare);
            Trace.Assert(resultBitsShare.Count == bitCount + 1);
        }
        else {
            // Drop additional carry up bit
            Trace.Assert(resultBitsShare.Count == bitCount);
        }

        return resultBitsShare;
    });

    private async Task<List<bool>> BitsAdd(IReadOnlyList<bool> leftBitsShare, IReadOnlyList<bool> rightBitsShare, string keyPrefix, bool preserveOverflow = false) => await AsyncHelper.TerminateOnException(async () => {
        if (leftBitsShare.Count != rightBitsShare.Count) {
            throw new Exception("leftBitsShare.Count != rightBitsShare.Count");
        }

        int bitCount = leftBitsShare.Count;
        List<bool> resultBitsShare = [];
        bool carryShare = false;

        for (int i = 0; i < bitCount; i++) {
            bool aBitShare = leftBitsShare[i];
            bool bBitShare = rightBitsShare[i];

            resultBitsShare.Add(aBitShare ^ bBitShare ^ carryShare);

            // Update carryShare
            bool t1 = await this.BeaverBitwiseAnd(aBitShare, bBitShare, $"{keyPrefix}_{i}_1");
            bool t2 = await this.BeaverBitwiseAnd(aBitShare, carryShare, $"{keyPrefix}_{i}_2");
            bool t3 = await this.BeaverBitwiseAnd(bBitShare, carryShare, $"{keyPrefix}_{i}_3");
            bool t4 = await this.BeaverBitwiseOr(t1, t2, $"{keyPrefix}_{i}_4");
            bool t5 = await this.BeaverBitwiseOr(t4, t3, $"{keyPrefix}_{i}_5");
            carryShare = t5;
        }

        if (preserveOverflow) {
            resultBitsShare.Add(carryShare);
            Trace.Assert(resultBitsShare.Count == bitCount + 1);
        }
        else {
            // Drop additional carry up bit
            Trace.Assert(resultBitsShare.Count == bitCount);
        }

        return resultBitsShare;
    });

    private async Task<List<bool>> LowBit(IReadOnlyList<bool> bits, string keyPrefix) => await AsyncHelper.TerminateOnException(async () => {
        // int lowBit(int a) => a & (~a + 1)
        int bitCount = bits.Count;

        List<bool> one = Enumerable.Repeat(false, bitCount).ToList();
        one[0] = true;

        IReadOnlyList<bool> a = bits;
        IReadOnlyList<bool> notA = a.Select(bit => this.BitwiseNot(bit)).ToList();
        IReadOnlyList<bool> negA = await this.BitsAddConst(notA, one, $"{keyPrefix}_Neg");

        List<bool> ret = [];
        for (int i = 0; i < bitCount; i++) {
            ret.Add(await this.BeaverBitwiseAnd(a[i], negA[i], $"{keyPrefix}_{i}"));
        }
        return ret;
    });

}

