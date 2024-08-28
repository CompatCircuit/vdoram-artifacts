using SadPencil.CompatCircuitCore.GlobalConfig;
using SadPencil.CompatCircuitProgramming.CircuitElements;
using SadPencil.CompatCircuitProgramming.Gadgets;

namespace SadPencil.CollaborativeZkVm.ZkVmCircuits;
public class InstructionFetchCircuitBoardGenerator(int opcodeTableLength) : ICircuitBoardGenerator {
    public int OpcodeTableLength = opcodeTableLength > 0 ? opcodeTableLength : throw new ArgumentOutOfRangeException(nameof(opcodeTableLength), "must be a positive integer");

    public CircuitBoard GetCircuitBoard() {
        int opcodeTableLength = this.OpcodeTableLength;
        CircuitBoard circuitBoard = new();

        // Op types that should be revealed
        List<ZkVmOpType> specialOps = [ZkVmOpType.Halt, ZkVmOpType.PublicInput, ZkVmOpType.PrivateInput, ZkVmOpType.PublicOutput];

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

        #region const sequential numbers
        // Prepare constant number i
        IReadOnlyDictionary<int, Wire> constNumberWires;
        {
            Dictionary<int, Wire> constNumberWireDict = [];
            void AddConstNumberWire(int val) {
                if (!constNumberWireDict.ContainsKey(val)) {
                    Wire wire = Wire.NewConstantWire(ArithConfig.FieldFactory.New(val), $"const_number_{val}");
                    circuitBoard.AddWire(wire);
                    constNumberWireDict.Add(val, wire);
                }
            }

            for (int i = 0; i < this.OpcodeTableLength; i++) {
                AddConstNumberWire(i);
            }

            AddConstNumberWire((int)ZkVmOpType.Halt);
            AddConstNumberWire((int)ZkVmOpType.PrivateInput);
            AddConstNumberWire((int)ZkVmOpType.PublicInput);
            AddConstNumberWire((int)ZkVmOpType.PublicOutput);

            constNumberWires = constNumberWireDict;
        }
        #endregion

        #region in_global_step_counter
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

        // Public input: opcode table rows
        List<Wire> opcodeTableArg0Wires = [];
        List<Wire> opcodeTableArg1Wires = [];
        List<Wire> opcodeTableArg2Wires = [];
        List<Wire> opcodeTableOpWires = [];
        for (int i = 0; i < opcodeTableLength; i++) {
            // Public input: opcode_table_i_arg0
            {
                Wire wire = Wire.NewPublicInputWire($"opcode_table_{i}_arg0");
                circuitBoard.AddWire(wire);
                opcodeTableArg0Wires.Add(wire);
            }
            // Public input: opcode_table_i_arg1
            {
                Wire wire = Wire.NewPublicInputWire($"opcode_table_{i}_arg1");
                circuitBoard.AddWire(wire);
                opcodeTableArg1Wires.Add(wire);
            }
            // Public input: opcode_table_i_arg2
            {
                Wire wire = Wire.NewPublicInputWire($"opcode_table_{i}_arg2");
                circuitBoard.AddWire(wire);
                opcodeTableArg2Wires.Add(wire);
            }
            // Public input: opcode_table_i_op
            {
                Wire wire = Wire.NewPublicInputWire($"opcode_table_{i}_op");
                circuitBoard.AddWire(wire);
                opcodeTableOpWires.Add(wire);
            }
        }

        // Fetch the correct opcode table row
        List<Wire> lineSelectionBitWires = [];
        for (int i = 0; i < opcodeTableLength; i++) {
            Wire selectionBitWire;
            {
                GadgetInstance ins = new FieldEqualsGadget().ApplyGadget([constNumberWires[i], thisProgramCounterSafe], $"opcode_table_selection_bit_{i}()");
                ins.Save(circuitBoard);
                selectionBitWire = ins.OutputWires[0];
                selectionBitWire.Name = $"opcode_table_selection_bit_{i}";
            }
            lineSelectionBitWires.Add(selectionBitWire);
        }

        Wire outOpcodeTableArg0Wire;
        {
            GadgetInstance ins = new SelectComposeGadget(opcodeTableLength).ApplyGadget(
                [.. lineSelectionBitWires, .. opcodeTableArg0Wires], "out_opcode_table_arg0()");
            ins.Save(circuitBoard);
            outOpcodeTableArg0Wire = ins.OutputWires[0];
            outOpcodeTableArg0Wire.Name = "out_opcode_table_arg0";
            outOpcodeTableArg0Wire.IsPrivateOutput = true;
        }

        Wire outOpcodeTableArg1Wire;
        {
            GadgetInstance ins = new SelectComposeGadget(opcodeTableLength).ApplyGadget(
                [.. lineSelectionBitWires, .. opcodeTableArg1Wires], "out_opcode_table_arg1()");
            ins.Save(circuitBoard);
            outOpcodeTableArg1Wire = ins.OutputWires[0];
            outOpcodeTableArg1Wire.Name = "out_opcode_table_arg1";
            outOpcodeTableArg1Wire.IsPrivateOutput = true;
        }

        Wire outOpcodeTableArg2Wire;
        {
            GadgetInstance ins = new SelectComposeGadget(opcodeTableLength).ApplyGadget(
                               [.. lineSelectionBitWires, .. opcodeTableArg2Wires], "out_opcode_table_arg2()");
            ins.Save(circuitBoard);
            outOpcodeTableArg2Wire = ins.OutputWires[0];
            outOpcodeTableArg2Wire.Name = "out_opcode_table_arg2";
            outOpcodeTableArg2Wire.IsPrivateOutput = true;
        }

        // If all selection bits are 0, the program counter is out of range
        Wire outOfRangeWire = Wire.NewConstantWire(ArithConfig.FieldFactory.NegOne, "const_number_program_counter_out_of_range");
        circuitBoard.AddWire(outOfRangeWire);

        Wire outOpcodeTableOpWire;
        {
            GadgetInstance ins = new SelectComposeOtherwiseGadget(opcodeTableLength).ApplyGadget(
                [.. lineSelectionBitWires, .. opcodeTableOpWires, outOfRangeWire], "out_opcode_table_op()");
            ins.Save(circuitBoard);
            outOpcodeTableOpWire = ins.OutputWires[0];
            outOpcodeTableOpWire.Name = "out_opcode_table_op";
            outOpcodeTableOpWire.IsPrivateOutput = true;
        }

        // Const: const_hash_optable_row_magic
        Wire constHashOptableRowMagicWire = Wire.NewConstantWire(MagicNumber.HashOptableRowMagic, "const_hash_optable_row_magic");
        circuitBoard.AddWire(constHashOptableRowMagicWire);

        // Public output: out_hash_opcode_table_row = H(magic, global_step_counter, opcode_table_i_op, opcode_table_i_arg0, opcode_table_i_arg1, opcode_table_i_arg2, enc_key)
        Wire outHashOpcodeTableRowWire;
        {
            List<Wire> preimageWires = [
                constHashOptableRowMagicWire,
                inGlobalStepCounter,
                outOpcodeTableOpWire,
                outOpcodeTableArg0Wire,
                outOpcodeTableArg1Wire,
                outOpcodeTableArg2Wire,
                inEncKeyWire];
            GadgetInstance ins = MimcHashGadget.GetGadgetWithDefaultParams(preimageWires.Count).ApplyGadget(preimageWires, "out_hash_opcode_table_row()");
            ins.Save(circuitBoard);
            outHashOpcodeTableRowWire = ins.OutputWires[0];
            outHashOpcodeTableRowWire.Name = "out_hash_opcode_table_row";
            outHashOpcodeTableRowWire.IsPublicOutput = true;
        }

        // Reveal special opcodes
        List<Wire> isOpcodeEqualsSpecialOpWires = [];
        for (int i = 0; i < specialOps.Count; i++) {
            Wire isOpcodeEqualsSpecialOpWire;
            {
                GadgetInstance ins = new FieldEqualsGadget().ApplyGadget([outOpcodeTableOpWire, constNumberWires[(int)specialOps[i]]], $"is_opcode_equals_special_op_{i}()");
                ins.Save(circuitBoard);
                isOpcodeEqualsSpecialOpWire = ins.OutputWires[0];
                isOpcodeEqualsSpecialOpWire.Name = $"is_opcode_equals_special_op_{i}";
            }
            isOpcodeEqualsSpecialOpWires.Add(isOpcodeEqualsSpecialOpWire);
        }

        // Public output: out_is_op_revealed
        Wire outIsOpRevealedWire;
        {
            // isOpcodeEqualsSpecialOpWires are always exclusive. So we can use addition to sum them up. The result is either 0 or 1.
            GadgetInstance ins = new FieldAddGadget(isOpcodeEqualsSpecialOpWires.Count).ApplyGadget(isOpcodeEqualsSpecialOpWires, $"out_is_op_revealed()");
            ins.Save(circuitBoard);
            outIsOpRevealedWire = ins.OutputWires[0];
            outIsOpRevealedWire.Name = "out_is_op_revealed";
            outIsOpRevealedWire.IsPublicOutput = true;
        }

        // Public output: out_op_if_revealed
        Wire outOpIfRevealedWire;
        {
            GadgetInstance ins = new FieldMulGadget().ApplyGadget([outIsOpRevealedWire, outOpcodeTableOpWire], "out_op_if_revealed()");
            ins.Save(circuitBoard);
            outOpIfRevealedWire = ins.OutputWires[0];
            outOpIfRevealedWire.Name = "out_op_if_revealed";
            outOpIfRevealedWire.IsPublicOutput = true;
        }

        return circuitBoard;
    }
}
