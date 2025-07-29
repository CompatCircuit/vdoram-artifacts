using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitProgramming.CircuitElements;
using Anonymous.CompatCircuitProgramming.Gadgets;
using System.Diagnostics;
using System.Numerics;

namespace Anonymous.CollaborativeZkVm.ZkVmCircuits;

public class ZkVmExecutorCircuitBoardGenerator : ICircuitBoardGenerator {
    public int HotRegCount { get; }
    public int RegCount { get; }
    public ZkVmExecutorCircuitBoardGenerator() : this(regCount: 16) { }
    public ZkVmExecutorCircuitBoardGenerator(int regCount) : this(regCount, regCount) { }

    private ZkVmExecutorCircuitBoardGenerator(int regCount, int hotRegCount) {
        this.HotRegCount = hotRegCount > 0 ? hotRegCount : throw new ArgumentOutOfRangeException(nameof(hotRegCount), "must be a positive integer");
        this.RegCount = regCount >= hotRegCount ? regCount : throw new ArgumentOutOfRangeException(nameof(regCount), "must be greater or equal than hot register count");
    }

    public CircuitBoard GetCircuitBoard() {
        CircuitBoard circuitBoard = new();
        IReadOnlyList<ZkVmOpType> AllZkVmOpTypes = Enum.GetValues<ZkVmOpType>();

        #region const sequential numbers
        // Prepare constant number i and power of twos
        IReadOnlyDictionary<int, Wire> constNumberWires;
        IReadOnlyList<Wire> twoPowWires;
        {
            Dictionary<int, Wire> constNumberWireDict = [];
            Wire AddConstNumberWire(int val) {
                if (!constNumberWireDict.ContainsKey(val)) {
                    Wire wire = Wire.NewConstantWire(ArithConfig.FieldFactory.New(val), $"const_number_{val}");
                    circuitBoard.AddWire(wire);
                    constNumberWireDict.Add(val, wire);
                    return wire;
                }
                return constNumberWireDict[val];
            }

            _ = AddConstNumberWire(0);
            _ = AddConstNumberWire(1);
            _ = AddConstNumberWire(2);

            for (int i = 0; i < this.RegCount; i++) {
                _ = AddConstNumberWire(i);
            }

            foreach (ZkVmOpType opType in AllZkVmOpTypes) {
                _ = AddConstNumberWire((int)opType);
            }

            constNumberWires = constNumberWireDict;

            // prepare power of two wires
            List<Wire> twoPowWiresList = [];
            twoPowWiresList.Add(constNumberWires[1]); // 2^0 = 1
            twoPowWiresList.Add(constNumberWires[2]); // 2^1 = 2

            BigInteger twoPowValue = 2;
            for (int i = 2; i < ArithConfig.BitSize; i++) {
                twoPowValue *= 2;
                Wire twoPowWire;
                if (twoPowValue < int.MaxValue) {
                    twoPowWire = AddConstNumberWire((int)twoPowValue);
                }
                else {
                    twoPowWire = Wire.NewConstantWire(ArithConfig.FieldFactory.New(twoPowValue), $"const_number_{twoPowValue}");
                    circuitBoard.AddWire(twoPowWire);
                }
                twoPowWiresList.Add(twoPowWire);
            }

            twoPowWires = twoPowWiresList;
        }

        Wire constNumberNegOneWire = Wire.NewConstantWire(ArithConfig.FieldFactory.NegOne, "const_number_neg_one");
        circuitBoard.AddWire(constNumberNegOneWire);
        #endregion

        #region in_enc_key
        // Const: const_hash_key_magic
        Wire constHashEncKeyMagicWire = Wire.NewConstantWire(MagicNumber.HashEncKeyMagic, "const_hash_enc_key_magic");
        circuitBoard.AddWire(constHashEncKeyMagicWire);

        // Private input: in_enc_key
        Wire inEncKeyWire = Wire.NewPrivateInputWire("in_enc_key");
        circuitBoard.AddWire(inEncKeyWire);

        // Public output: out_hash_enc_key = H(magic, in_enc_key); must be consistent among all mem proofs and vm circuits (manually checked by verifiers)
        Wire outHashEncKeyWire;
        {
            List<Wire> preimageWires = [constHashEncKeyMagicWire, inEncKeyWire];
            GadgetInstance ins = MimcHashGadget.GetGadgetWithDefaultParams(preimageWires.Count).ApplyGadget(preimageWires, "out_hash_enc_key()");
            ins.Save(circuitBoard);
            outHashEncKeyWire = ins.OutputWires[0];
            outHashEncKeyWire.Name = "out_hash_enc_key";
            outHashEncKeyWire.IsPublicOutput = true;
        }
        #endregion

        #region global step counter
        // Public input: in_global_step_counter
        Wire inGlobalStepCounter = Wire.NewPublicInputWire("in_global_step_counter");
        circuitBoard.AddWire(inGlobalStepCounter);

        // is_global_step_counter_non_zero
        Wire isGlobalStepCounterNonZero;
        {
            GadgetInstance ins = new FieldNormGadget().ApplyGadget([inGlobalStepCounter], "is_global_step_counter_non_zero()");
            ins.Save(circuitBoard);
            isGlobalStepCounterNonZero = ins.OutputWires[0];
            isGlobalStepCounterNonZero.Name = "is_global_step_counter_non_zero";
        }

        // global_step_counter_minus_one = in_global_step_counter - 1
        Wire globalStepCounterMinusOne;
        {
            GadgetInstance ins = new FieldSubGadget().ApplyGadget([inGlobalStepCounter, constNumberWires[1]], "global_step_counter_minus_one()");
            ins.Save(circuitBoard);
            globalStepCounterMinusOne = ins.OutputWires[0];
            globalStepCounterMinusOne.Name = "global_step_counter_minus_one";
        }
        #endregion

        // Public input: in_public_input
        Wire inPublicInputWire = Wire.NewPublicInputWire("in_public_input");
        circuitBoard.AddWire(inPublicInputWire);

        // Private input: in_private_input
        Wire inPrivateInputWire = Wire.NewPrivateInputWire("in_private_input");
        circuitBoard.AddWire(inPrivateInputWire);

        #region program counter
        // Private input: in_this_program_counter
        Wire inThisProgramCounter = Wire.NewPrivateInputWire("in_this_program_counter");
        circuitBoard.AddWire(inThisProgramCounter);

        // this_program_counter_safe = 0 if in_global_step_counter is 0 else in_this_program_counter
        Wire thisProgramCounterSafe;
        {
            GadgetInstance ins = new FieldMulGadget().ApplyGadget([isGlobalStepCounterNonZero, inThisProgramCounter], "this_program_counter_safe()");
            ins.Save(circuitBoard);
            thisProgramCounterSafe = ins.OutputWires[0];
            thisProgramCounterSafe.Name = "this_program_counter_safe";
        }

        // Const: const_hash_program_counter_magic
        Wire constHashProgramCounterMagicWire = Wire.NewConstantWire(MagicNumber.HashProgramCounterMagic, "const_hash_program_counter_magic");
        circuitBoard.AddWire(constHashProgramCounterMagicWire);

        // hash_this_program_counter_raw = H(magic, global_step_counter - 1, this_program_counter_safe, enc_key)
        Wire hashThisProgramCounterRawWire;
        {
            List<Wire> preimageWires = [constHashProgramCounterMagicWire, globalStepCounterMinusOne, thisProgramCounterSafe, inEncKeyWire];
            GadgetInstance ins = MimcHashGadget.GetGadgetWithDefaultParams(preimageWires.Count).ApplyGadget(preimageWires, "hash_this_program_counter_raw()");
            ins.Save(circuitBoard);
            hashThisProgramCounterRawWire = ins.OutputWires[0];
            hashThisProgramCounterRawWire.Name = "hash_this_program_counter_raw";
        }

        // out_hash_this_program_counter = 0 if global_step_counter is 0 else hash_this_program_counter_raw
        Wire outHashThisProgramCounterWire;
        {
            GadgetInstance ins = new FieldMulGadget().ApplyGadget([isGlobalStepCounterNonZero, hashThisProgramCounterRawWire], "out_hash_this_program_counter()");
            ins.Save(circuitBoard);
            outHashThisProgramCounterWire = ins.OutputWires[0];
            outHashThisProgramCounterWire.Name = "out_hash_this_program_counter";
            outHashThisProgramCounterWire.IsPublicOutput = true;
        }
        #endregion

        #region registers
        IReadOnlyList<Wire> inRegisterWires;
        {
            List<Wire> inRegisterWireList = [];
            for (int i = 0; i < this.RegCount; i++) {
                Wire inThisRegWire = Wire.NewPrivateInputWire($"in_this_reg_{i}");
                circuitBoard.AddWire(inThisRegWire);

                // in_this_reg_i_safe = 0 if in_global_step_counter is 0 else in_this_reg_i
                Wire inThisRegSafeWire;
                {
                    GadgetInstance ins = new FieldMulGadget().ApplyGadget([isGlobalStepCounterNonZero, inThisRegWire], $"in_this_reg_{i}_safe()");
                    ins.Save(circuitBoard);
                    inThisRegSafeWire = ins.OutputWires[0];
                    inThisRegSafeWire.Name = $"in_this_reg_{i}_safe";
                }

                inRegisterWireList.Add(inThisRegSafeWire);
            }

            inRegisterWires = inRegisterWireList;
        }

        // Public output: out_hash_this_registers
        Wire constHashRegistersMagicWire = Wire.NewConstantWire(MagicNumber.HashRegistersMagic, "const_hash_registers_magic");
        circuitBoard.AddWire(constHashRegistersMagicWire);

        // hash_this_registers_raw = H(magic, global_step_counter - 1, in_this_reg0_safe, ..., in_this_reg7_safe, enc_key)
        Wire hashThisRegistersRawWire;
        {
            List<Wire> preimageWires = [constHashRegistersMagicWire, globalStepCounterMinusOne, .. inRegisterWires, inEncKeyWire];
            GadgetInstance ins = MimcHashGadget.GetGadgetWithDefaultParams(preimageWires.Count).ApplyGadget(preimageWires, "hash_this_registers_raw()");
            ins.Save(circuitBoard);
            hashThisRegistersRawWire = ins.OutputWires[0];
            hashThisRegistersRawWire.Name = "hash_this_registers_raw";
        }

        // out_hash_this_registers = 0 if global_step_counter is 0 else hash_this_registers_raw
        Wire outHashThisRegistersWire;
        {
            GadgetInstance ins = new FieldMulGadget().ApplyGadget([isGlobalStepCounterNonZero, hashThisRegistersRawWire], "out_hash_this_registers()");
            ins.Save(circuitBoard);
            outHashThisRegistersWire = ins.OutputWires[0];
            outHashThisRegistersWire.Name = "out_hash_this_registers";
            outHashThisRegistersWire.IsPublicOutput = true;
        }
        #endregion

        // Private input: (memory reading) in_mem_val
        Wire inMemValWire = Wire.NewPrivateInputWire("in_mem_val");
        circuitBoard.AddWire(inMemValWire);

        // Private input: (memory reading) in_mem_addr (should be reg0)
        // Wire inMemAddrWire = Wire.NewPrivateInputWire("in_mem_addr");
        // circuitBoard.AddWire(inMemAddrWire);
        Wire inMemAddrWire = inRegisterWires[0];

        #region opcode table

        Wire inOpcodeTableOpWire = Wire.NewPrivateInputWire("in_opcode_table_op");
        circuitBoard.AddWire(inOpcodeTableOpWire);

        Wire inOpcodeTableArg0Wire = Wire.NewPrivateInputWire("in_opcode_table_arg0");
        circuitBoard.AddWire(inOpcodeTableArg0Wire);

        Wire inOpcodeTableArg1Wire = Wire.NewPrivateInputWire("in_opcode_table_arg1");
        circuitBoard.AddWire(inOpcodeTableArg1Wire);

        Wire inOpcodeTableArg2Wire = Wire.NewPrivateInputWire("in_opcode_table_arg2");
        circuitBoard.AddWire(inOpcodeTableArg2Wire);

        // Const: const_hash_optable_row_magic
        Wire constHashOptableRowMagicWire = Wire.NewConstantWire(MagicNumber.HashOptableRowMagic, "const_hash_optable_row_magic");
        circuitBoard.AddWire(constHashOptableRowMagicWire);

        // Public output: out_hash_opcode_table_row = H(magic, global_step_counter, opcode_table_i_op, opcode_table_i_arg0, opcode_table_i_arg1, opcode_table_i_arg2, enc_key)
        // (manually verified by verifiers)
        Wire outHashOpcodeTableRowWire;
        {
            List<Wire> preimageWires = [constHashOptableRowMagicWire,
                inGlobalStepCounter,
                inOpcodeTableOpWire,
                inOpcodeTableArg0Wire,
                inOpcodeTableArg1Wire,
                inOpcodeTableArg2Wire,
                inEncKeyWire];
            GadgetInstance ins = MimcHashGadget.GetGadgetWithDefaultParams(preimageWires.Count).ApplyGadget(preimageWires, "out_hash_opcode_table_row()");
            ins.Save(circuitBoard);
            outHashOpcodeTableRowWire = ins.OutputWires[0];
            outHashOpcodeTableRowWire.Name = "out_hash_opcode_table_row";
            outHashOpcodeTableRowWire.IsPublicOutput = true;
        }
        #endregion

        // Select reg{arg0}, reg{arg1}, reg{arg2}
        Wire inRegAtArg0;
        Wire inRegAtArg1;
        Wire inRegAtArg2;
        IReadOnlyList<Wire> isArg0EqualsConstNumberWires;
        IReadOnlyList<Wire> isArg1EqualsConstNumberWires;
        IReadOnlyList<Wire> isArg2EqualsConstNumberWires;
        {
            List<Wire> isArg0EqualsConstNumberWireList = [];
            List<Wire> isArg1EqualsConstNumberWireList = [];
            List<Wire> isArg2EqualsConstNumberWireList = [];
            for (int i = 0; i < this.RegCount; i++) {
                {
                    Wire wire;
                    GadgetInstance ins = new FieldEqualsGadget().ApplyGadget([constNumberWires[i], inOpcodeTableArg0Wire], $"is_arg0_equals_const_number_{i}()");
                    ins.Save(circuitBoard);
                    wire = ins.OutputWires[0];
                    wire.Name = $"is_arg0_equals_const_number_{i}";
                    isArg0EqualsConstNumberWireList.Add(wire);
                }
                {
                    Wire wire;
                    GadgetInstance ins = new FieldEqualsGadget().ApplyGadget([constNumberWires[i], inOpcodeTableArg1Wire], $"is_arg1_equals_const_number_{i}()");
                    ins.Save(circuitBoard);
                    wire = ins.OutputWires[0];
                    wire.Name = $"is_arg1_equals_const_number_{i}";
                    isArg1EqualsConstNumberWireList.Add(wire);
                }
                {
                    Wire wire;
                    GadgetInstance ins = new FieldEqualsGadget().ApplyGadget([constNumberWires[i], inOpcodeTableArg2Wire], $"is_arg2_equals_const_number_{i}()");
                    ins.Save(circuitBoard);
                    wire = ins.OutputWires[0];
                    wire.Name = $"is_arg2_equals_const_number_{i}";
                    isArg2EqualsConstNumberWireList.Add(wire);
                }
            }

            isArg0EqualsConstNumberWires = isArg0EqualsConstNumberWireList;
            isArg1EqualsConstNumberWires = isArg1EqualsConstNumberWireList;
            isArg2EqualsConstNumberWires = isArg2EqualsConstNumberWireList;

            // Select reg{arg0}
            {
                GadgetInstance ins = new SelectComposeGadget(this.RegCount).ApplyGadget([.. isArg0EqualsConstNumberWires, .. inRegisterWires], "in_reg_at_arg0()");
                ins.Save(circuitBoard);
                inRegAtArg0 = ins.OutputWires[0];
                inRegAtArg0.Name = "in_reg_at_arg0";
            }

            // Select reg{arg1}
            {
                GadgetInstance ins = new SelectComposeGadget(this.RegCount).ApplyGadget([.. isArg1EqualsConstNumberWires, .. inRegisterWires], "in_reg_at_arg1()");
                ins.Save(circuitBoard);
                inRegAtArg1 = ins.OutputWires[0];
                inRegAtArg1.Name = "in_reg_at_arg1";
            }

            // Select reg{arg2}
            {
                GadgetInstance ins = new SelectComposeGadget(this.RegCount).ApplyGadget([.. isArg2EqualsConstNumberWires, .. inRegisterWires], "in_reg_at_arg2()");
                ins.Save(circuitBoard);
                inRegAtArg2 = ins.OutputWires[0];
                inRegAtArg2.Name = "in_reg_at_arg2";
            }
        }

        // Process each op

        // Prepare is_table_op_equals_i
        Dictionary<ZkVmOpType, Wire> isTableOpEqualsOpTypeWires = [];
        foreach (ZkVmOpType opType in AllZkVmOpTypes) {
            int opTypeID = (int)opType;
            Wire wire;
            {
                GadgetInstance ins = new FieldEqualsGadget().ApplyGadget([inOpcodeTableOpWire, constNumberWires[opTypeID]], $"is_table_op_equals_{opTypeID}()");
                ins.Save(circuitBoard);
                wire = ins.OutputWires[0];
                wire.Name = $"is_table_op_equals_{opTypeID}";
            }
            isTableOpEqualsOpTypeWires.Add(opType, wire);
        }

        // OpType -> program_counter
        // An OpType can only use one of the following possibilities:
        // 1. OpType-> true
        HashSet<ZkVmOpType> virtualOutProgramCounterByDefaultPossibilities = [];
        // 2. OpType -> program_counter
        // Used by: JumpIfZero, Halt
        Dictionary<ZkVmOpType, Wire> virtualOutProgramCounterPossibilities = [];

        // The following 3 dictionaries are used to store the possibilities of the output registers
        // An OpType can only use one of the following possibilities:

        // 1. OpType -> true
        // Used by OpType: Halt, PublicOutput, JumpIfZero
        HashSet<ZkVmOpType> virtualOutAllRegistersNoTouchPossibilities = [];

        // 2. OpType -> [reg0, reg1, reg2, ..., reg{RegCount - 1}]
        // Used by OpType: Shift
        Dictionary<ZkVmOpType, IReadOnlyList<Wire>> virtualOutAllRegistersPossibilities = [];

        // 3. OpType-> [reg{arg0}, reg{arg1}]
        // Used by OpType: Swap, Move
        // Note: arg0 must be different from arg1
        Dictionary<ZkVmOpType, Tuple<Wire, Wire>> virtualOutRegAtArg01Possibilities = [];

        // 4. OpType-> [reg{arg0}]
        // Restriction: arg0 should in range [0, HotRegCount - 1]
        Dictionary<ZkVmOpType, Wire> virtualOutHotRegAtArg0Possibilities = [];

        Wire defaultOutProgramCounter;
        {
            // default_out_pc = in_pc + 1
            GadgetInstance ins = new FieldAddGadget().ApplyGadget([thisProgramCounterSafe, constNumberWires[1]], "default_out_pc()");
            ins.Save(circuitBoard);
            defaultOutProgramCounter = ins.OutputWires[0];
            defaultOutProgramCounter.Name = "default_out_pc";
        }

        // ZkVmOpType.Halt
        {
            ZkVmOpType opType = ZkVmOpType.Halt;
            virtualOutProgramCounterPossibilities.Add(opType, thisProgramCounterSafe);
            _ = virtualOutAllRegistersNoTouchPossibilities.Add(opType);
        }

        // ZkVmOpType.Shift
        {
            ZkVmOpType opType = ZkVmOpType.Shift;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);

            List<Wire> outRegisterWiresCopy = inRegisterWires.ToList();
            for (int i = 0; i < this.RegCount; i++) {
                outRegisterWiresCopy[(i + 1) % this.RegCount] = inRegisterWires[i];
            }
            virtualOutAllRegistersPossibilities.Add(opType, outRegisterWiresCopy);
        }

        // ZkVmOpType.Swap
        {
            ZkVmOpType opType = ZkVmOpType.Swap;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            virtualOutRegAtArg01Possibilities.Add(opType, Tuple.Create(inRegAtArg1, inRegAtArg0));
        }

        // ZkVmOpType.Move
        {
            ZkVmOpType opType = ZkVmOpType.Move;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            virtualOutRegAtArg01Possibilities.Add(opType, Tuple.Create(inRegAtArg1, inRegAtArg1));
        }

        // ZkVmOpType.Set
        {
            ZkVmOpType opType = ZkVmOpType.Set;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            virtualOutHotRegAtArg0Possibilities.Add(opType, inOpcodeTableArg1Wire);
        }

        // ZkVmOpType.PublicInput
        {
            ZkVmOpType opType = ZkVmOpType.PublicInput;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            virtualOutHotRegAtArg0Possibilities.Add(opType, inPublicInputWire);
        }

        // ZkVmOpType.PrivateInput
        {
            ZkVmOpType opType = ZkVmOpType.PrivateInput;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            virtualOutHotRegAtArg0Possibilities.Add(opType, inPrivateInputWire);
        }

        // ZkVmOpType.PublicOutput
        {
            ZkVmOpType opType = ZkVmOpType.PublicOutput;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            _ = virtualOutAllRegistersNoTouchPossibilities.Add(opType);

            // Set public output from Reg{arg0}
            // Public output: out_public_output
            Wire outPublicOutputWire; // = inRegisterWires[0] * isTableOpEquals
            {
                GadgetInstance ins = new FieldMulGadget().ApplyGadget(
                    [inRegAtArg0, isTableOpEqualsOpTypeWires[opType]], "out_public_output()");
                ins.Save(circuitBoard);
                outPublicOutputWire = ins.OutputWires[0];
                outPublicOutputWire.Name = "out_public_output";
                outPublicOutputWire.IsPublicOutput = true;
            }
        }

        // ZkVmOpType.Noop
        {
            ZkVmOpType opType = ZkVmOpType.Noop;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            _ = virtualOutAllRegistersNoTouchPossibilities.Add(opType);
        }

        // ZkVmOpType.JumpIfZero
        {
            ZkVmOpType opType = ZkVmOpType.JumpIfZero;
            Wire isRegAtArg0NonZero;
            {
                GadgetInstance ins = new FieldNormGadget().ApplyGadget([inRegAtArg0], "is_reg_at_arg0_non_zero()");
                ins.Save(circuitBoard);
                isRegAtArg0NonZero = ins.OutputWires[0];
                isRegAtArg0NonZero.Name = "is_reg_at_arg0_non_zero";
            }

            Wire isRegAtArg0Zero;
            {
                GadgetInstance ins = new BoolNotGadget().ApplyGadget([isRegAtArg0NonZero], "is_reg_at_arg0_zero()");
                ins.Save(circuitBoard);
                isRegAtArg0Zero = ins.OutputWires[0];
                isRegAtArg0Zero.Name = "is_reg_at_arg0_zero";
            }

            // Determine the next pc: default if reg{arg0} is non-zero, arg1 if reg{arg0} is zero
            Wire possibleOutPcWire;
            {
                GadgetInstance ins = new SelectComposeGadget(2).ApplyGadget(
                    [isRegAtArg0Zero, isRegAtArg0NonZero, inOpcodeTableArg1Wire, defaultOutProgramCounter], "possible_out_pc()");
                ins.Save(circuitBoard);
                possibleOutPcWire = ins.OutputWires[0];
                possibleOutPcWire.Name = "possible_out_pc";
            }

            virtualOutProgramCounterPossibilities.Add(opType, possibleOutPcWire);
            _ = virtualOutAllRegistersNoTouchPossibilities.Add(opType);
        }

        // ZkVmOpType.Store
        // Store value (reg{arg0}) to memory address (reg0)
        // Note: out_mem_val and out_mem_addr are handled in memory proof region
        {
            ZkVmOpType opType = ZkVmOpType.Store;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            virtualOutAllRegistersPossibilities.Add(opType, inRegisterWires);
        }

        // ZkVmOpType.Load
        // Set reg0 from memory address (reg0)
        // Note: the correctness is verified via out_hash_memproof, which is handled in memory proof region
        {
            ZkVmOpType opType = ZkVmOpType.Load;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            virtualOutHotRegAtArg0Possibilities.Add(opType, inMemValWire);
        }

        // ZkVmOpType.Hash
        {
            ZkVmOpType opType = ZkVmOpType.Hash;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);

            Wire hashRegAtArg1Wire;
            {
                List<Wire> preimageWires = [inRegAtArg1];
                GadgetInstance ins = MimcHashGadget.GetGadgetWithDefaultParams(preimageWires.Count).ApplyGadget(preimageWires, "hash_reg_at_arg1()");
                ins.Save(circuitBoard);
                hashRegAtArg1Wire = ins.OutputWires[0];
                hashRegAtArg1Wire.Name = "hash_reg_at_arg1";
            }

            virtualOutHotRegAtArg0Possibilities.Add(opType, hashRegAtArg1Wire);
        }

        // ZkVmOpType.Add
        {
            ZkVmOpType opType = ZkVmOpType.Add;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            Wire addResultWire;
            {
                GadgetInstance ins = new FieldAddGadget().ApplyGadget([inRegAtArg1, inRegAtArg2], "add_result()");
                ins.Save(circuitBoard);
                addResultWire = ins.OutputWires[0];
                addResultWire.Name = "add_result";
            }
            virtualOutHotRegAtArg0Possibilities.Add(opType, addResultWire);
        }

        // ZkVmOpType.Sub
        {
            ZkVmOpType opType = ZkVmOpType.Sub;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            Wire subResultWire;
            {
                GadgetInstance ins = new FieldSubGadget().ApplyGadget([inRegAtArg1, inRegAtArg2], "sub_result()");
                ins.Save(circuitBoard);
                subResultWire = ins.OutputWires[0];
                subResultWire.Name = "sub_result";
            }
            virtualOutHotRegAtArg0Possibilities.Add(opType, subResultWire);
        }

        // ZkVmOpType.Mul
        {
            ZkVmOpType opType = ZkVmOpType.Mul;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            Wire mulResultWire;
            {
                GadgetInstance ins = new FieldMulGadget().ApplyGadget([inRegAtArg1, inRegAtArg2], "mul_result()");
                ins.Save(circuitBoard);
                mulResultWire = ins.OutputWires[0];
                mulResultWire.Name = "mul_result";
            }
            virtualOutHotRegAtArg0Possibilities.Add(opType, mulResultWire);
        }

        // ZkVmOpType.Inv
        {
            ZkVmOpType opType = ZkVmOpType.Inv;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            Wire invResultWire;
            {
                GadgetInstance ins = new FieldInverseGadget().ApplyGadget([inRegAtArg1], "inv_result()");
                ins.Save(circuitBoard);
                invResultWire = ins.OutputWires[0];
                invResultWire.Name = "inv_result";
            }
            virtualOutHotRegAtArg0Possibilities.Add(opType, invResultWire);
        }

        // ZkVmOpType.Norm
        {
            ZkVmOpType opType = ZkVmOpType.Norm;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            Wire normResultWire;
            {
                GadgetInstance ins = new FieldNormGadget().ApplyGadget([inRegAtArg1], "norm_result()");
                ins.Save(circuitBoard);
                normResultWire = ins.OutputWires[0];
                normResultWire.Name = "norm_result";
            }
            virtualOutHotRegAtArg0Possibilities.Add(opType, normResultWire);
        }

        // ZkVmOpType.And
        {
            ZkVmOpType opType = ZkVmOpType.And;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            Wire andResultWire;
            {
                GadgetInstance ins = new BoolAndGadget().ApplyGadget([inRegAtArg1, inRegAtArg2], "and_result()");
                ins.Save(circuitBoard);
                andResultWire = ins.OutputWires[0];
                andResultWire.Name = "and_result";
            }
            virtualOutHotRegAtArg0Possibilities.Add(opType, andResultWire);
        }

        // ZkVmOpType.Or
        {
            ZkVmOpType opType = ZkVmOpType.Or;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            Wire orResultWire;
            {
                GadgetInstance ins = new BoolOrGadget().ApplyGadget([inRegAtArg1, inRegAtArg2], "or_result()");
                ins.Save(circuitBoard);
                orResultWire = ins.OutputWires[0];
                orResultWire.Name = "or_result";
            }
            virtualOutHotRegAtArg0Possibilities.Add(opType, orResultWire);
        }

        // ZkVmOpType.Xor
        {
            ZkVmOpType opType = ZkVmOpType.Xor;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            Wire xorResultWire;
            {
                GadgetInstance ins = new BoolXorGadget().ApplyGadget([inRegAtArg1, inRegAtArg2], "xor_result()");
                ins.Save(circuitBoard);
                xorResultWire = ins.OutputWires[0];
                xorResultWire.Name = "xor_result";
            }
            virtualOutHotRegAtArg0Possibilities.Add(opType, xorResultWire);
        }

        // ZkVmOpType.Not
        {
            ZkVmOpType opType = ZkVmOpType.Not;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            Wire notResultWire;
            {
                GadgetInstance ins = new BoolNotGadget().ApplyGadget([inRegAtArg1], "not_result()");
                ins.Save(circuitBoard);
                notResultWire = ins.OutputWires[0];
                notResultWire.Name = "not_result";
            }
            virtualOutHotRegAtArg0Possibilities.Add(opType, notResultWire);
        }

        // ZkVmOpType.LessThan
        IReadOnlyList<Wire> inRegAtArg1Bits;
        {
            GadgetInstance ins = new BitDecompositionGadget().ApplyGadget([inRegAtArg1], "in_reg_at_arg_1_bits()");
            ins.Save(circuitBoard);
            inRegAtArg1Bits = ins.OutputWires;
            for (int i = 0; i < inRegAtArg1Bits.Count; i++) {
                inRegAtArg1Bits[i].Name = $"in_reg_at_arg_1_bits_{i}";
            }
        }

        IReadOnlyList<Wire> inRegAtArg2Bits;
        {
            GadgetInstance ins = new BitDecompositionGadget().ApplyGadget([inRegAtArg2], "in_reg_at_arg_2_bits()");
            ins.Save(circuitBoard);
            inRegAtArg2Bits = ins.OutputWires;
            for (int i = 0; i < inRegAtArg2Bits.Count; i++) {
                inRegAtArg2Bits[i].Name = $"in_reg_at_arg_2_bits_{i}";
            }
        }

        Trace.Assert(ArithConfig.BitSize == inRegAtArg1Bits.Count);
        Trace.Assert(ArithConfig.BitSize == inRegAtArg2Bits.Count);

        {
            ZkVmOpType opType = ZkVmOpType.LessThan;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            Wire lessThanResultWire;
            {
                GadgetInstance ins = new BitsLessThanGadget(ArithConfig.BitSize).ApplyGadget([.. inRegAtArg1Bits, .. inRegAtArg2Bits], "less_than_result()");
                ins.Save(circuitBoard);
                lessThanResultWire = ins.OutputWires[0];
                lessThanResultWire.Name = "less_than_result";
            }
            virtualOutHotRegAtArg0Possibilities.Add(opType, lessThanResultWire);
        }

        // ZkVmOpType.RightShift
        {
            ZkVmOpType opType = ZkVmOpType.RightShift;
            _ = virtualOutProgramCounterByDefaultPossibilities.Add(opType);
            Wire rightShiftResultWire;
            {
                List<Wire> itemWires = [];

                for (int i = 0; i < ArithConfig.BitSize - 1; i++) { // -1: Since we right shift by 1
                    Wire leftBit = inRegAtArg1Bits[i + 1]; // +1: Since we right shift by 1
                    Wire twoPowWire = twoPowWires[i]; // 2^i

                    Wire itemWire;
                    {
                        GadgetInstance ins = new FieldMulGadget().ApplyGadget([leftBit, twoPowWire], $"in_reg_at_arg_2_bits_{i + 1}_2^{i}()"); // +1: Since we right shift by 1
                        ins.Save(circuitBoard);
                        itemWire = ins.OutputWires[0];
                        itemWire.Name = $"in_reg_at_arg_2_bits_{i + 1}_2^{i}";
                    }

                    itemWires.Add(itemWire);
                }

                {
                    GadgetInstance ins = new FieldAddGadget(itemWires.Count).ApplyGadget(itemWires, "right_shift_one_result()");
                    ins.Save(circuitBoard);
                    rightShiftResultWire = ins.OutputWires[0];
                    rightShiftResultWire.Name = "right_shift_one_result";
                }
            }

            virtualOutHotRegAtArg0Possibilities.Add(opType, rightShiftResultWire);
        }

        // Check program counter possibilities
        Trace.Assert(AllZkVmOpTypes.Count == virtualOutProgramCounterPossibilities.Count + virtualOutProgramCounterByDefaultPossibilities.Count);
        foreach (ZkVmOpType opType in AllZkVmOpTypes) {
            int matchCount = 0;
            if (virtualOutProgramCounterPossibilities.ContainsKey(opType)) {
                matchCount++;
            }

            if (virtualOutProgramCounterByDefaultPossibilities.Contains(opType)) {
                matchCount++;
            }

            Trace.Assert(matchCount == 1);
        }

        // Check register possibilities
        Trace.Assert(AllZkVmOpTypes.Count ==
            virtualOutAllRegistersPossibilities.Count + virtualOutHotRegAtArg0Possibilities.Count + virtualOutRegAtArg01Possibilities.Count + virtualOutAllRegistersNoTouchPossibilities.Count);
        foreach (ZkVmOpType opType in AllZkVmOpTypes) {
            int matchCount = 0;
            if (virtualOutAllRegistersNoTouchPossibilities.Contains(opType)) {
                matchCount++;
            }

            if (virtualOutAllRegistersPossibilities.ContainsKey(opType)) {
                matchCount++;
            }

            if (virtualOutHotRegAtArg0Possibilities.ContainsKey(opType)) {
                matchCount++;
            }

            if (virtualOutRegAtArg01Possibilities.ContainsKey(opType)) {
                matchCount++;
            }

            Trace.Assert(matchCount == 1);
        }

        // Compose private output (out_program_counter) based on these possibilities
        Wire outProgramCounterWire;
        {
            List<Wire> programCounterConditions = [];
            List<Wire> programCounterChoices = [];
            foreach ((ZkVmOpType opType, Wire wire) in virtualOutProgramCounterPossibilities) {
                programCounterConditions.Add(isTableOpEqualsOpTypeWires[opType]);
                programCounterChoices.Add(wire);
            }

            Wire programCounterOthewiseChoice = defaultOutProgramCounter;

            // Select out_program_counter based on these possibilities
            {
                GadgetInstance ins = new SelectComposeOtherwiseGadget(programCounterConditions.Count).ApplyGadget(
                    [.. programCounterConditions, .. programCounterChoices, programCounterOthewiseChoice], "out_program_counter()");
                ins.Save(circuitBoard);
                outProgramCounterWire = ins.OutputWires[0];
                outProgramCounterWire.Name = "out_program_counter";
                outProgramCounterWire.IsPrivateOutput = true;
            }
        }

        // Compose private output (out_reg_i) based on these possibilities
        List<Wire> outRegWires = [];
        for (int i = 0; i < this.RegCount; i++) {
            List<Wire> regConditions = [];
            List<Wire> regChoices = [];

            // virtualOutHotRegAtArg0Possibilities
            if (i < this.HotRegCount) {
                foreach ((ZkVmOpType opType, Wire arg0Wire) in virtualOutHotRegAtArg0Possibilities) {
                    int opTypeID = (int)opType;
                    // condition: isTableOpEqualsOpTypeWires[opType] AND isArg0EqualsConstNumberWires[i]
                    Wire conditionWire;
                    {
                        GadgetInstance ins = new BoolAndGadget().ApplyGadget([isTableOpEqualsOpTypeWires[opType], isArg0EqualsConstNumberWires[i]], $"reg_{i}_condition_{opTypeID}_arg0()");
                        ins.Save(circuitBoard);
                        conditionWire = ins.OutputWires[0];
                        conditionWire.Name = $"reg_{i}_condition_{opTypeID}_arg0";
                    }
                    regConditions.Add(conditionWire);
                    regChoices.Add(arg0Wire);
                }
            }

            // virtualOutRegAtArg01Possibilities
            foreach ((ZkVmOpType opType, (Wire arg0Wire, Wire arg1Wire)) in virtualOutRegAtArg01Possibilities) {
                int opTypeID = (int)opType;
                // arg0 condition: isTableOpEqualsOpTypeWires[opType] AND isArg0EqualsConstNumberWires[i]
                Wire arg0ConditionWire;
                {
                    GadgetInstance ins = new BoolAndGadget().ApplyGadget([isTableOpEqualsOpTypeWires[opType], isArg0EqualsConstNumberWires[i]], $"reg_{i}_condition_{opTypeID}_arg0()");
                    ins.Save(circuitBoard);
                    arg0ConditionWire = ins.OutputWires[0];
                    arg0ConditionWire.Name = $"reg_{i}_condition_{opTypeID}_arg0";
                }
                regConditions.Add(arg0ConditionWire);
                regChoices.Add(arg0Wire);

                // arg1 condition: (NOT arg0 condition) AND isTableOpEqualsOpTypeWires[opType] AND isArg1EqualsConstNumberWires[i]
                Wire arg0ConditionNotWire;
                {
                    GadgetInstance ins = new BoolNotGadget().ApplyGadget([arg0ConditionWire], $"reg_{i}_condition_{opTypeID}_arg0_not()");
                    ins.Save(circuitBoard);
                    arg0ConditionNotWire = ins.OutputWires[0];
                    arg0ConditionNotWire.Name = $"reg_{i}_condition_{opTypeID}_arg0_not";
                }
                Wire arg1ConditionWire;
                {
                    GadgetInstance ins = new BoolAndGadget(3).ApplyGadget([arg0ConditionNotWire, isTableOpEqualsOpTypeWires[opType], isArg1EqualsConstNumberWires[i]], $"reg_{i}_condition_{opTypeID}_arg1()");
                    ins.Save(circuitBoard);
                    arg1ConditionWire = ins.OutputWires[0];
                    arg1ConditionWire.Name = $"reg_{i}_condition_{opTypeID}_arg1";
                }
                regConditions.Add(arg1ConditionWire);
                regChoices.Add(arg1Wire);
            }

            // virtualOutAllRegistersPossibilities
            foreach ((ZkVmOpType opType, IReadOnlyList<Wire> outRegisterWires) in virtualOutAllRegistersPossibilities) {
                int opTypeID = (int)opType;

                // condition: isTableOpEqualsOpTypeWires[opType]
                regConditions.Add(isTableOpEqualsOpTypeWires[opType]);
                regChoices.Add(outRegisterWires[i]);
            }

            // virtualOutAllRegistersNoTouchPossibilities (i.e., otherwise)
            Wire regOtherwiseChoice = inRegisterWires[i];

            Trace.Assert(regChoices.Count == regConditions.Count);

            // Select out_reg_i based on these possibilities
            Wire outRegisterWire;
            {
                GadgetInstance ins = new SelectComposeOtherwiseGadget(regConditions.Count).ApplyGadget(
                    [.. regConditions, .. regChoices, regOtherwiseChoice], $"out_reg_{i}()");
                ins.Save(circuitBoard);
                outRegisterWire = ins.OutputWires[0];
                outRegisterWire.Name = $"out_reg_{i}";
                outRegisterWire.IsPrivateOutput = true;
            }
            outRegWires.Add(outRegisterWire);
        }

        // Public output: out_hash_next_program_counter = H(magic, global_step_counter, out_program_counter, enc_key)
        Wire outHashNextProgramCounterWire;
        {
            List<Wire> preimageWires = [constHashProgramCounterMagicWire, inGlobalStepCounter, outProgramCounterWire, inEncKeyWire];
            GadgetInstance ins = MimcHashGadget.GetGadgetWithDefaultParams(preimageWires.Count).ApplyGadget(preimageWires, "out_hash_next_program_counter()");
            ins.Save(circuitBoard);
            outHashNextProgramCounterWire = ins.OutputWires[0];
            outHashNextProgramCounterWire.Name = "out_hash_next_program_counter";
            outHashNextProgramCounterWire.IsPublicOutput = true;
        }

        // Public output: out_hash_next_registers = H(magic, global_step_counter, out_reg0, ..., out_reg7, enc_key)
        Wire outNextHashPcRegsWire;
        {
            List<Wire> preimageWires = [constHashRegistersMagicWire, inGlobalStepCounter, .. outRegWires, inEncKeyWire];
            GadgetInstance ins = MimcHashGadget.GetGadgetWithDefaultParams(preimageWires.Count).ApplyGadget(preimageWires, "out_hash_next_registers()");
            ins.Save(circuitBoard);
            outNextHashPcRegsWire = ins.OutputWires[0];
            outNextHashPcRegsWire.Name = "out_hash_next_registers";
            outNextHashPcRegsWire.IsPublicOutput = true;
        }

        // Public output: out_halt
        Wire outHaltWire;
        {
            outHaltWire = isTableOpEqualsOpTypeWires[ZkVmOpType.Halt];
            outHaltWire.Name = "out_halt";
            outHaltWire.IsPublicOutput = true;
        }

        // Public output: out_error
        // Return 1 if unexpected error happens.
        // currently, the only possible error is that the opcode is NegOne (i.e., invalid opcode, reported by instruction fetcher)
        Wire outErrorWire;
        {
            Wire isOpNegOneWire;
            {
                GadgetInstance ins = new FieldEqualsGadget().ApplyGadget([inOpcodeTableOpWire, constNumberNegOneWire], "is_op_neg_one()");
                ins.Save(circuitBoard);
                isOpNegOneWire = ins.OutputWires[0];
                isOpNegOneWire.Name = "is_op_neg_one";
            }

            outErrorWire = isOpNegOneWire;
            outErrorWire.Name = "out_error";
            outErrorWire.IsPublicOutput = true;
        }

        #region memory operation results
        // Private output: out_mem_val and out_mem_addr
        Wire outMemValWire;
        Wire outMemAddrWire;
        {
            // out_mem_val = reg{arg0} if op is ZkVmOpType.Store else 0
            {
                GadgetInstance ins = new FieldMulGadget().ApplyGadget(
                    [isTableOpEqualsOpTypeWires[ZkVmOpType.Store], inRegAtArg0], "out_mem_val()");
                ins.Save(circuitBoard);
                outMemValWire = ins.OutputWires[0];
                outMemValWire.Name = "out_mem_val";
                outMemValWire.IsPrivateOutput = true;
            }

            Wire defaultMemAddrWire;
            {
                // out_mem_addr = reg0 if is_table_op_equals_op_type else NegOne
                defaultMemAddrWire = constNumberNegOneWire;
            }

            Wire isTableOpNotStoreWire;
            {
                GadgetInstance ins = new BoolNotGadget().ApplyGadget([isTableOpEqualsOpTypeWires[ZkVmOpType.Store]], "is_table_op_not_store()");
                ins.Save(circuitBoard);
                isTableOpNotStoreWire = ins.OutputWires[0];
                isTableOpNotStoreWire.Name = "is_table_op_not_store";
            }

            // out_mem_addr
            {
                GadgetInstance ins = new SelectComposeGadget(2).ApplyGadget(
                    [isTableOpNotStoreWire, isTableOpEqualsOpTypeWires[ZkVmOpType.Store], defaultMemAddrWire, inRegisterWires[0]], "out_mem_addr()");
                ins.Save(circuitBoard);
                outMemAddrWire = ins.OutputWires[0];
                outMemAddrWire.Name = "out_mem_addr";
                outMemAddrWire.IsPrivateOutput = true;
            }
        }
        #endregion

        #region memory proof

        {
            // Private output: out_trace_is_mem_op: op is ZkVmOpType.Load or ZkVmOpType.Store
            Wire outTraceIsMemOpWire;
            {
                GadgetInstance ins = new BoolOrGadget().ApplyGadget([isTableOpEqualsOpTypeWires[ZkVmOpType.Load], isTableOpEqualsOpTypeWires[ZkVmOpType.Store]], "out_trace_is_mem_op()");
                ins.Save(circuitBoard);
                outTraceIsMemOpWire = ins.OutputWires[0];
                outTraceIsMemOpWire.Name = "out_trace_is_mem_op";
                outTraceIsMemOpWire.IsPrivateOutput = true;
            }

            // Private output: out_trace_is_mem_write: op is ZkVmOpType.Store
            Wire outTraceIsMemWriteWire = isTableOpEqualsOpTypeWires[ZkVmOpType.Store];
            outTraceIsMemWriteWire.Name = "out_trace_is_mem_write"; // the original name is not used, so overwrite it
            outTraceIsMemWriteWire.IsPrivateOutput = true;

            Wire constHashMemTraceMagicWire = Wire.NewConstantWire(MagicNumber.HashMemTraceMagic, "const_hash_mem_trace_magic");
            circuitBoard.AddWire(constHashMemTraceMagicWire);

            // Private output: out_trace_mem_addr and out_trace_mem_val.
            // They are output values if op is ZkVmOpType.Store, input values if op is ZkVmOpType.Load,
            // otherwise, the address is NegOne and the value is 0.
            Wire outTraceMemAddrWire;
            {
                GadgetInstance ins = new SelectComposeOtherwiseGadget(2).ApplyGadget(
                    [isTableOpEqualsOpTypeWires[ZkVmOpType.Store], isTableOpEqualsOpTypeWires[ZkVmOpType.Load], outMemAddrWire, inMemAddrWire, constNumberNegOneWire], "out_trace_mem_addr()");
                ins.Save(circuitBoard);
                outTraceMemAddrWire = ins.OutputWires[0];
                outTraceMemAddrWire.Name = "out_trace_mem_addr";
                outTraceMemAddrWire.IsPrivateOutput = true;
            }

            Wire outTraceMemValWire;
            {
                GadgetInstance ins = new SelectComposeOtherwiseGadget(2).ApplyGadget(
                    [isTableOpEqualsOpTypeWires[ZkVmOpType.Store], isTableOpEqualsOpTypeWires[ZkVmOpType.Load], outMemValWire, inMemValWire, constNumberWires[0]], "out_trace_mem_val()");
                ins.Save(circuitBoard);
                outTraceMemValWire = ins.OutputWires[0];
                outTraceMemValWire.Name = "out_trace_mem_val";
                outTraceMemValWire.IsPrivateOutput = true;
            }

            // Compute out_hash_mem_trace = H(magic, global_step_counter, trace_is_mem_op, trace_is_mem_write, trace_mem_addr, trace_mem_val, enc_key)
            Wire outHashMemTraceWire;
            {
                List<Wire> preimages = [constHashMemTraceMagicWire, inGlobalStepCounter, outTraceIsMemOpWire, outTraceIsMemWriteWire, outTraceMemAddrWire, outTraceMemValWire, inEncKeyWire];
                GadgetInstance ins = MimcHashGadget.GetGadgetWithDefaultParams(preimages.Count).ApplyGadget(preimages, "out_hash_mem_trace()");
                ins.Save(circuitBoard);
                outHashMemTraceWire = ins.OutputWires[0];
                outHashMemTraceWire.Name = "out_hash_mem_trace";
                outHashMemTraceWire.IsPublicOutput = true;
            }
        }
        #endregion

        return circuitBoard;
    }
}