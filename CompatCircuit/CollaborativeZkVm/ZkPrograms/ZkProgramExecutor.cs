using SadPencil.CollaborativeZkVm.ZkVmCircuits;
using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.CompatCircuits;
using SadPencil.CompatCircuitCore.CompatCircuits.R1csCircuits;
using SadPencil.CompatCircuitCore.Computation;
using SadPencil.CompatCircuitCore.Extensions;
using SadPencil.CompatCircuitCore.GlobalConfig;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives;
using SadPencil.CompatCircuitProgramming.CircuitElements;
using System.Diagnostics;
using System.Numerics;

namespace SadPencil.CollaborativeZkVm.ZkPrograms;
public class ZkProgramExecutor {
    // TODO: extract ZkProgramExecutor as well as ExperimentExecutor as an interface

    public required IProgress<(string, R1csCircuitWithValues)> OnR1csCircuitWithValuesGenerated;

    public required ZkProgramInstance ZkProgramInstance { get; init; }

    public int RegisterCount { get; } = 16;

    public required IMpcExecutorFactory MpcExecutorFactory { get; init; }

    public required bool IsSingleParty { get; init; }
    public required int MyID { get; init; }

    public int GlobalStepNoMoreThan => this.ZkProgramInstance.GlobalStepsNoMoreThan;

    private MpcValue MpcShareFromExistingShare(Field valueShare) =>
        this.IsSingleParty ? new MpcValue(valueShare, isSecretShare: false) : new MpcValue(valueShare, isSecretShare: true);

    private MpcValue MpcShareFromPublicValue(Field value) =>
        this.IsSingleParty ? new(value, isSecretShare: false) : new(this.MyID == 0 ? value : ArithConfig.FieldFactory.Zero, isSecretShare: true);

    public async Task<ZkProgramExecuteResult> Execute() => await AsyncHelper.TerminateOnException(async () => {
        DateTimeOffset totalStartTime = DateTimeOffset.Now;
        Dictionary<string, DateTimeOffset> stepStartTimes = [];
        Dictionary<string, DateTimeOffset> stepEndTimes = [];

        // Prepare zkVM
        int regCount = this.RegisterCount;
        ZkProgramInstance program = this.ZkProgramInstance;

        IEnumerator<Field> publicInputEnumerator = this.ZkProgramInstance.PublicInputs.GetEnumerator();
        IEnumerator<Field> privateInputShareEnumerator = this.ZkProgramInstance.PrivateInputShares.GetEnumerator();

        CircuitBoard zkVmCircuitBoard = new ZkVmExecutorCircuitBoardGenerator(regCount).GetCircuitBoard().Optimize();
        CircuitBoard instructionFetcherCircuitBoard = new InstructionFetchCircuitBoardGenerator(program.Opcodes.Count).GetCircuitBoard().Optimize();

        // Each party randomly generates a key share for the encryption key
        MpcValue inEncKey = this.MpcShareFromExistingShare(ArithConfig.FieldFactory.Random());
        Field? pubOutHashEncKey = null;

        MpcValue zeroShare = this.MpcShareFromPublicValue(ArithConfig.FieldFactory.Zero);

        MpcValue thisProgramCounter = zeroShare;
        Field hashThisProgramCounter = ArithConfig.FieldFactory.Zero;

        List<MpcValue> thisRegisters = Enumerable.Repeat(zeroShare, regCount).ToList();
        Field hashThisRegisters = ArithConfig.FieldFactory.Zero;

        List<Field> publicOutputs = [];
        void AddPublicOutputResult(Field value) => publicOutputs.Add(value);

        // This list is supposed to be private (accessible only among MPC parties)
        List<MpcValue> privOutMemValFields = [];

        // The following (memoryTraceColumnNames, memoryTrace, hashMemoryTraceCount) are exclusive to MemoryModel.MtpMemoryTrace

        IReadOnlyList<string> memoryTraceColumnNames = MemoryTraceFetchCircuitBoardGenerator.ColumnNames;

        // row -> column -> value
        // Note: we assume that globalStepCounter is ordered. As long as MPC parties honestly behave, this assumption holds.
        List<IReadOnlyList<MpcValue>> memoryTrace = [];
        void AddMemoryTrace(IReadOnlyDictionary<string, MpcValue> incomingTraceRow) {
            foreach (string columnName in memoryTraceColumnNames) {
                if (!incomingTraceRow.ContainsKey(columnName)) {
                    throw new Exception($"Incoming trace row does not contain column {columnName}");
                }
            }

            List<MpcValue> incomingList = [];
            for (int i = 0; i < memoryTraceColumnNames.Count; i++) {
                incomingList.Add(incomingTraceRow[memoryTraceColumnNames[i]]);
            }
            memoryTrace.Add(incomingList);
        }

        Dictionary<Field, int> hashMemoryTraceCount = [];
        void AddHashMemoryTrace(Field hash) {
            if (!hashMemoryTraceCount.ContainsKey(hash)) {
                hashMemoryTraceCount[hash] = 0;
            }
            hashMemoryTraceCount[hash]++;
        }

        // End of MemoryModel.MtpMemoryTrace exclusive

        int globalStepCounter;

        // Execute the program
        for (globalStepCounter = 0; ; globalStepCounter++) {
            Serilog.Log.Information($"Global Step: {globalStepCounter}");
            if (this.GlobalStepNoMoreThan != -1 && globalStepCounter > this.GlobalStepNoMoreThan) {
                throw new Exception("Global step counter exceeds the limit");
            }

            Field globalStepCounterField = ArithConfig.FieldFactory.New(globalStepCounter);

            MpcValue privOutOpcodeTableOp;
            MpcValue privOutOpcodeTableArg0;
            MpcValue privOutOpcodeTableArg1;
            MpcValue privOutOpcodeTableArg2;
            Field pubOutHashOpcodeTableRow;

            Field pubOutIsOpRevealed;
            Field pubOutOpIfRevealed;

            // 1. Instruction Fetch
            // Fetch out_opcode_table_op, out_opcode_table_arg0, out_opcode_table_arg1, out_opcode_table_arg2 and out_hash_opcode_table_row
            {
                string stepTimerName = $"IF-{globalStepCounter}";
                stepStartTimes[stepTimerName] = DateTimeOffset.Now;

                CircuitBoardMpcExecutorWrapper executorWrapper;
                {
                    CircuitBoardConverter.ToCompatCircuit(instructionFetcherCircuitBoard, "InstructionFetcherCircuit", out CompatCircuit compatCircuit, out CompatCircuitSymbols compatCircuitSymbols);
                    executorWrapper = new CircuitBoardMpcExecutorWrapper(compatCircuit, compatCircuitSymbols, this.MpcExecutorFactory.NextExecutor());
                }
                Action<string, Field> AddPublic = executorWrapper.AddPublic;
                Action<string, MpcValue> AddPrivate = executorWrapper.AddPrivate;
                Func<Task> Compute = executorWrapper.Compute;
                Func<string, MpcValue> GetOutput = executorWrapper.GetOutput;

                AddPublic("in_global_step_counter", globalStepCounterField);

                for (int i = 0; i < program.Opcodes.Count; i++) {
                    Field opcodeTableOp = ArithConfig.FieldFactory.New((int)program.Opcodes[i].OpType);
                    Field opcodeTableArg0 = program.Opcodes[i].Arg0;
                    Field opcodeTableArg1 = program.Opcodes[i].Arg1;
                    Field opcodeTableArg2 = program.Opcodes[i].Arg2;

                    AddPublic($"opcode_table_{i}_op", opcodeTableOp);
                    AddPublic($"opcode_table_{i}_arg0", opcodeTableArg0);
                    AddPublic($"opcode_table_{i}_arg1", opcodeTableArg1);
                    AddPublic($"opcode_table_{i}_arg2", opcodeTableArg2);
                }

                AddPrivate("in_enc_key", inEncKey);
                AddPrivate("in_this_program_counter", thisProgramCounter);

                await Compute();

                privOutOpcodeTableOp = GetOutput("out_opcode_table_op");
                privOutOpcodeTableArg0 = GetOutput("out_opcode_table_arg0");
                privOutOpcodeTableArg1 = GetOutput("out_opcode_table_arg1");
                privOutOpcodeTableArg2 = GetOutput("out_opcode_table_arg2");
                pubOutHashOpcodeTableRow = GetOutput("out_hash_opcode_table_row").AssumeNonShare();
                {
                    Field newOutHashThisProgramCounter = GetOutput("out_hash_this_program_counter").AssumeNonShare();
                    if (hashThisProgramCounter != newOutHashThisProgramCounter) {
                        throw new Exception("Hash of current program counter is inconsistent");
                    }
                }
                {
                    Field newOutHashEncKey = GetOutput("out_hash_enc_key").AssumeNonShare();
                    pubOutHashEncKey ??= newOutHashEncKey;
                    if (pubOutHashEncKey != newOutHashEncKey) {
                        throw new Exception("Hash of the encryption key is inconsistent");
                    }
                }

                pubOutIsOpRevealed = GetOutput("out_is_op_revealed").AssumeNonShare();
                pubOutOpIfRevealed = GetOutput("out_op_if_revealed").AssumeNonShare();

                // save r1cs circuit with value shares for verification
                this.OnR1csCircuitWithValuesGenerated.Report(($"InstructionFetcherCircuit-Step-{globalStepCounter}", executorWrapper.GetR1csCircuitWithValues()));

                stepEndTimes[stepTimerName] = DateTimeOffset.Now;
            }

            // 2. Fetch memory value

            MpcValue memVal;
            {
                string stepTimerName = $"MF-{globalStepCounter}";
                stepStartTimes[stepTimerName] = DateTimeOffset.Now;

                Trace.Assert(globalStepCounter == privOutMemValFields.Count);

                if (globalStepCounter == 0) {
                    memVal = zeroShare;
                }
                else {
                    CircuitBoard memoryTraceFetcherCircuitBoard = new MemoryTraceFetchCircuitBoardGenerator(globalStepCounter).GetCircuitBoard().Optimize();
                    CircuitBoardMpcExecutorWrapper executorWrapper; // TODO: CircuitBoardPureMpcExecutorWrapper
                    {
                        CircuitBoardConverter.ToCompatCircuit(memoryTraceFetcherCircuitBoard, $"MemoryTraceFetcherCircuit-{globalStepCounter}", out CompatCircuit compatCircuit, out CompatCircuitSymbols compatCircuitSymbols);
                        executorWrapper = new CircuitBoardMpcExecutorWrapper(compatCircuit, compatCircuitSymbols, this.MpcExecutorFactory.NextExecutor());
                    }

                    Action<string, Field> AddPublic = executorWrapper.AddPublic;
                    Action<string, MpcValue> AddPrivate = executorWrapper.AddPrivate;
                    Func<Task> Compute = executorWrapper.Compute;
                    Func<string, MpcValue> GetOutput = executorWrapper.GetOutput;

                    Trace.Assert(globalStepCounter == memoryTrace.Count);
                    for (int col = 0; col < memoryTraceColumnNames.Count; col++) {
                        string colName = memoryTraceColumnNames[col];
                        for (int row = 0; row < memoryTrace.Count; row++) {
                            AddPrivate($"in_trace_{row}_{colName}", memoryTrace[row][col]);
                        }
                    }

                    // memory address is stored in reg0
                    AddPrivate("in_mem_addr", thisRegisters[0]);

                    await Compute();

                    memVal = GetOutput($"out_trace_mem_val");

                }
                stepEndTimes[stepTimerName] = DateTimeOffset.Now;
            }

            Field pubOutputThisStep;

            // 3. Execute
            {
                string stepTimerName = $"IE-{globalStepCounter}";
                stepStartTimes[stepTimerName] = DateTimeOffset.Now;

                CircuitBoardMpcExecutorWrapper executorWrapper;
                {
                    CircuitBoardConverter.ToCompatCircuit(zkVmCircuitBoard, "ZkVmCircuit", out CompatCircuit compatCircuit, out CompatCircuitSymbols compatCircuitSymbols);
                    executorWrapper = new CircuitBoardMpcExecutorWrapper(compatCircuit, compatCircuitSymbols, this.MpcExecutorFactory.NextExecutor());
                }

                Action<string, Field> AddPublic = executorWrapper.AddPublic;
                Action<string, MpcValue> AddPrivate = executorWrapper.AddPrivate;
                Func<Task> Compute = executorWrapper.Compute;
                Func<string, MpcValue> GetOutput = executorWrapper.GetOutput;

                // Provide public/private inputs
                if (pubOutIsOpRevealed == ArithConfig.FieldFactory.One) {
                    if (pubOutOpIfRevealed == ArithConfig.FieldFactory.New((int)ZkVmOpType.PublicInput)) {
                        bool hasValue = publicInputEnumerator.MoveNext();
                        if (!hasValue) {
                            throw new Exception("Insufficient public inputs");
                        }
                        AddPublic("in_public_input", publicInputEnumerator.Current);
                    }
                    else {
                        AddPublic("in_public_input", ArithConfig.FieldFactory.Zero);
                    }

                    if (pubOutOpIfRevealed == ArithConfig.FieldFactory.New((int)ZkVmOpType.PrivateInput)) {
                        bool hasValue = privateInputShareEnumerator.MoveNext();
                        if (!hasValue) {
                            throw new Exception("Insufficient private inputs");
                        }
                        AddPrivate("in_private_input", this.MpcShareFromExistingShare(privateInputShareEnumerator.Current));
                    }
                    else {
                        AddPrivate("in_private_input", zeroShare);
                    }
                }
                else {
                    AddPublic("in_public_input", ArithConfig.FieldFactory.Zero);
                    AddPrivate("in_private_input", zeroShare);
                }

                AddPublic("in_global_step_counter", globalStepCounterField);
                AddPrivate("in_opcode_table_op", privOutOpcodeTableOp);
                AddPrivate("in_opcode_table_arg0", privOutOpcodeTableArg0);
                AddPrivate("in_opcode_table_arg1", privOutOpcodeTableArg1);
                AddPrivate("in_opcode_table_arg2", privOutOpcodeTableArg2);
                AddPrivate("in_enc_key", inEncKey);
                AddPrivate("in_this_program_counter", thisProgramCounter);

                // Add registers
                for (int i = 0; i < thisRegisters.Count; i++) {
                    AddPrivate($"in_this_reg_{i}", thisRegisters[i]);
                }

                // Add memory reading proof and value
                //AddPrivate("in_mem_row_index", memRowIndex);
                AddPrivate("in_mem_val", memVal);

                await Compute();

                // out_hash_enc_key
                {
                    Field newOutHashEncKey = GetOutput("out_hash_enc_key").AssumeNonShare();
                    if (pubOutHashEncKey != newOutHashEncKey) {
                        throw new Exception("Hash of the encryption key is inconsistent");
                    }
                }

                // out_hash_this_program_counter
                {
                    Field newOutHashThisProgramCounter = GetOutput("out_hash_this_program_counter").AssumeNonShare();
                    if (hashThisProgramCounter != newOutHashThisProgramCounter) {
                        throw new Exception("Hash of current program counter is inconsistent");
                    }
                }

                // out_hash_this_registers
                {
                    Field newOutHashThisRegisters = GetOutput("out_hash_this_registers").AssumeNonShare();
                    if (hashThisRegisters != newOutHashThisRegisters) {
                        throw new Exception("Hash of the registers is inconsistent");
                    }
                }

                // out_hash_opcode_table_row
                {
                    Field newOutHashOpcodeTableRow = GetOutput("out_hash_opcode_table_row").AssumeNonShare();
                    if (pubOutHashOpcodeTableRow != newOutHashOpcodeTableRow) {
                        throw new Exception("Hash of the opcode table row is inconsistent");
                    }
                }

                // out_halt
                Field pubOutHalt = GetOutput("out_halt").AssumeNonShare();

                // private output: out_mem_val
                {
                    MpcValue privOutMemVal = GetOutput("out_mem_val");
                    privOutMemValFields.Add(privOutMemVal);
                    // MPC parties should privately store this value. Will be used in MemorySecretValueFetcher
                }

                // Private output: out_mem_addr
                {
                    MpcValue privOutMemAddr = GetOutput("out_mem_addr");
                    // This output seems useless
                    // It equals the value of reg0 if Op is Load, otherwise 0
                }

                // out_hash_next_program_counter
                {
                    Field pubOutHashNextProgramCounter = GetOutput("out_hash_next_program_counter").AssumeNonShare();
                    hashThisProgramCounter = pubOutHashNextProgramCounter;
                }

                // Private output: out_program_counter
                {
                    MpcValue privOutProgramCounter = GetOutput("out_program_counter");
                    thisProgramCounter = privOutProgramCounter;
                }

                // out_hash_next_registers
                {
                    Field pubOutHashNextRegisters = GetOutput("out_hash_next_registers").AssumeNonShare();
                    hashThisRegisters = pubOutHashNextRegisters;
                }

                // Private output: out_reg_0 -- out_reg_{RegCount-1}
                {
                    List<MpcValue> privOutRegisters = Enumerable.Range(0, regCount).Select(i => GetOutput($"out_reg_{i}")).ToList();
                    thisRegisters = privOutRegisters;
                }

                // out_public_output
                {
                    pubOutputThisStep = GetOutput("out_public_output").AssumeNonShare();
                    if (pubOutIsOpRevealed == ArithConfig.FieldFactory.One && pubOutOpIfRevealed == ArithConfig.FieldFactory.New((int)ZkVmOpType.PublicOutput)) {
                        AddPublicOutputResult(pubOutputThisStep);
                    }
                }
                {
                    Field pubError = GetOutput("out_error").AssumeNonShare();
                    if (pubError != ArithConfig.FieldFactory.Zero) {
                        throw new Exception($"VM execution error");
                    }
                }

                // Save memory trace
                {
                    Dictionary<string, MpcValue> incomingTraceRow = [];
                    foreach (string colName in memoryTraceColumnNames) {
                        if (colName == "global_step_counter") {
                            incomingTraceRow.Add(colName, this.MpcShareFromPublicValue(globalStepCounterField));
                        }
                        else {
                            MpcValue traceValue = GetOutput($"out_trace_{colName}");
                            incomingTraceRow.Add(colName, traceValue);
                        }
                    }
                    AddMemoryTrace(incomingTraceRow);
                }

                // Save out_hash_mem_trace
                {
                    Field outHashMemTrace = GetOutput("out_hash_mem_trace").AssumeNonShare();
                    AddHashMemoryTrace(outHashMemTrace);
                }

                // save r1cs circuit with value shares for verification
                this.OnR1csCircuitWithValuesGenerated.Report(($"ZkVmCircuit-Step-{globalStepCounter}", executorWrapper.GetR1csCircuitWithValues()));

                stepEndTimes[stepTimerName] = DateTimeOffset.Now;

                if (pubOutHalt == ArithConfig.FieldFactory.One) {
                    break;
                }
            }

            if (this.IsSingleParty) {
                // Print pc and regs
                Serilog.Log.Information($"Global Step Counter: {globalStepCounter}");
                Serilog.Log.Information($"Next PC: {thisProgramCounter.AssumeNonShare()}");
                Serilog.Log.Information("Next Registers:");
                for (int i = 0; i < thisRegisters.Count; i++) {
                    Serilog.Log.Information($"Reg {i}: {thisRegisters[i].AssumeNonShare()}");
                }

                Serilog.Log.Information($"Is op revealed: {pubOutIsOpRevealed}");
                Serilog.Log.Information($"Op if revealed: {pubOutOpIfRevealed}");
                Serilog.Log.Information($"Public output: {pubOutputThisStep}");
                Serilog.Log.Information("");
            }
        }

        // Halted. 
        Serilog.Log.Information("Instruction execution completed. Sorting memory trace...");

        // 4. Sort memory trace
        List<List<MpcValue>> sortMemoryTrace;
        {
            string stepTimerName = $"TS-{globalStepCounter}";
            stepStartTimes[stepTimerName] = DateTimeOffset.Now;

            // Copy memory trace
            sortMemoryTrace = [];
            for (int i = 0; i < memoryTrace.Count; i++) {
                sortMemoryTrace.Add(memoryTrace[i].ToList());
            }

            // Pad memory trace
            int oldTraceCount = memoryTrace.Count;
            int newTraceCount = Convert.ToInt32(BitOperations.RoundUpToPowerOf2(Convert.ToUInt32(oldTraceCount)));
            {
                // fill with NegOne (the largest field value)
                for (int i = oldTraceCount; i < newTraceCount; i++) {
                    List<MpcValue> traceRow = [];
                    for (int j = 0; j < memoryTraceColumnNames.Count; j++) {
                        traceRow.Add(this.MpcShareFromPublicValue(ArithConfig.FieldFactory.NegOne));
                    }
                    sortMemoryTrace.Add(traceRow);
                }
            }

            // From now on, memoryTrace is not used anymore

            // Sort
            List<(int k, int j)> kjList = MemoryTraceSortInnerLoopCircuitBoardGenerator.GetAllLoopIndexKJ(newTraceCount).ToList();
            for (int kjIndex = 0; kjIndex < kjList.Count; kjIndex++) {
                (int k, int j) = kjList[kjIndex];
                Serilog.Log.Information($"[{(double)kjIndex / kjList.Count * 100:F2}%] Sorting: n{newTraceCount}-k{k}-j{j}");
                Serilog.Log.Information($"Compiling sort circuit...");
                CircuitBoard sortCircuitBoard = new MemoryTraceSortInnerLoopCircuitBoardGenerator(newTraceCount, k, j).GetCircuitBoard().Optimize();
                CircuitBoardMpcExecutorWrapper executorWrapper; // TODO: CircuitBoardPureMpcExecutorWrapper
                {
                    CircuitBoardConverter.ToCompatCircuit(sortCircuitBoard, $"MemoryTraceSorterCircuit-n{newTraceCount}-k{k}-j{j}", out CompatCircuit compatCircuit, out CompatCircuitSymbols compatCircuitSymbols);
                    executorWrapper = new CircuitBoardMpcExecutorWrapper(compatCircuit, compatCircuitSymbols, this.MpcExecutorFactory.NextExecutor());
                }

                Action<string, Field> AddPublic = executorWrapper.AddPublic;
                Action<string, MpcValue> AddPrivate = executorWrapper.AddPrivate;
                Func<Task> Compute = executorWrapper.Compute;
                Func<string, MpcValue> GetOutput = executorWrapper.GetOutput;

                for (int col = 0; col < memoryTraceColumnNames.Count; col++) {
                    string colName = memoryTraceColumnNames[col];
                    for (int row = 0; row < newTraceCount; row++) {
                        AddPrivate($"in_trace_{row}_{colName}", sortMemoryTrace[row][col]);
                    }
                }

                Serilog.Log.Information($"Executing sort circuit...");
                await Compute();

                // Save output traces
                // Note: since it's not the final loop, rows in [oldTraceCount, newTraceCount) should also be updated!
                for (int row = 0; row < newTraceCount; row++) {
                    for (int col = 0; col < memoryTraceColumnNames.Count; col++) {
                        sortMemoryTrace[row][col] = GetOutput($"out_trace_{row}_{memoryTraceColumnNames[col]}");
                    }
                }
            }

            // The padded rows should still be in the last part of the memory trace because they are the largest (NegOne)
            // The correctness is already handled in unit test.
            for (int row = newTraceCount - 1; row >= oldTraceCount; row--) {
                if (this.IsSingleParty) {
                    for (int col = 0; col < memoryTraceColumnNames.Count; col++) {
                        if (sortMemoryTrace[row][col].AssumeNonShare() != ArithConfig.FieldFactory.NegOne) {
                            throw new Exception("Unexpected sorted memory trace value.");
                        }
                    }
                }
                sortMemoryTrace.RemoveAt(row);
            }

            stepEndTimes[stepTimerName] = DateTimeOffset.Now;
        }

        // 5. Verify memory trace
        Serilog.Log.Information("Memory trace sort completed. Verifying memory trace...");
        // Compile MemoryTraceProveCircuitBoardGenerator
        {
            string stepTimerName = $"TV-{globalStepCounter}";
            stepStartTimes[stepTimerName] = DateTimeOffset.Now;

            int traceCount = sortMemoryTrace.Count;
            CircuitBoardMpcExecutorWrapper executorWrapper;
            {
                CircuitBoard proverCircuit = new MemoryTraceProveCircuitBoardGenerator(traceCount).GetCircuitBoard().Optimize();
                CircuitBoardConverter.ToCompatCircuit(proverCircuit, $"MemoryTraceProverCircuit-{traceCount}", out CompatCircuit compatCircuit, out CompatCircuitSymbols compatCircuitSymbols);
                executorWrapper = new CircuitBoardMpcExecutorWrapper(compatCircuit, compatCircuitSymbols, this.MpcExecutorFactory.NextExecutor());
            }

            Action<string, Field> AddPublic = executorWrapper.AddPublic;
            Action<string, MpcValue> AddPrivate = executorWrapper.AddPrivate;
            Func<Task> Compute = executorWrapper.Compute;
            Func<string, MpcValue> GetOutput = executorWrapper.GetOutput;

            AddPrivate("in_enc_key", inEncKey);

            for (int col = 0; col < memoryTraceColumnNames.Count; col++) {
                string colName = memoryTraceColumnNames[col];
                for (int row = 0; row < traceCount; row++) {
                    AddPrivate($"in_trace_{row}_{colName}", sortMemoryTrace[row][col]);
                }
            }

            await Compute();

            // out_hash_enc_key
            {
                Field newOutHashEncKey = GetOutput("out_hash_enc_key").AssumeNonShare();
                if (pubOutHashEncKey != newOutHashEncKey) {
                    throw new Exception("Hash of the encryption key is inconsistent");
                }
            }

            // out_is_satisfied
            {
                Field outIsSatisfied = GetOutput("out_is_satisfied").AssumeNonShare();
                if (outIsSatisfied != ArithConfig.FieldFactory.One) {
                    throw new Exception("Memory trace is not satisfied");
                }
            }

            // out_{i}_hash_mem_trace
            Dictionary<Field, int> outHashMemTraceCount = [];
            for (int i = 0; i < traceCount; i++) {
                Field outHashMemTrace = GetOutput($"out_{i}_hash_mem_trace").AssumeNonShare();
                if (!outHashMemTraceCount.ContainsKey(outHashMemTrace)) {
                    outHashMemTraceCount[outHashMemTrace] = 0;
                }
                outHashMemTraceCount[outHashMemTrace]++;
            }

            // Ensure that hash counts are consistent
            foreach ((Field hash, int expectedCount) in hashMemoryTraceCount) {
                if (outHashMemTraceCount.TryGetValue(hash, out int actualCount) && actualCount == expectedCount) {
                    continue;
                }
                throw new Exception($"Hash count mismatch for hash {hash}: expected {expectedCount}, actual {actualCount}");
            }

            // save r1cs circuit with value shares for verification
            this.OnR1csCircuitWithValuesGenerated.Report(($"MemoryTraceProverCircuit-{traceCount}", executorWrapper.GetR1csCircuitWithValues()));

            stepEndTimes[stepTimerName] = DateTimeOffset.Now;
        }

        DateTimeOffset totalEndTime = DateTimeOffset.Now;

        return new ZkProgramExecuteResult() {
            GlobalStepCounter = globalStepCounter,
            PublicOutputs = publicOutputs,
            TotalTime = totalEndTime - totalStartTime,
            StepTimes = stepStartTimes.Keys.Select(key => (key, stepEndTimes[key] - stepStartTimes[key])).ToDictionary(),
        };
    });
}
