using SadPencil.CompatCircuitCore.Extensions;
using SadPencil.CompatCircuitProgramming.CircuitElements;
using SadPencil.CompatCircuitProgramming.Gadgets;

namespace SadPencil.CollaborativeZkVm.ZkVmCircuits;
public class MemoryTraceProveCircuitBoardGenerator(int traceCount) : ICircuitBoardGenerator {
    public int TraceCount { get; } = traceCount > 0 ? traceCount : throw new ArgumentOutOfRangeException(nameof(traceCount), "must be a positive integer");
    public static IReadOnlyList<string> ColumnNames => MemoryTraceFetchCircuitBoardGenerator.ColumnNames;

    // https://docs.polygon.technology/zkEVM/architecture/zkprover/memory-sm/

    public CircuitBoard GetCircuitBoard() {
        CircuitBoard circuitBoard = new();
        int traceCount = this.TraceCount;

        int memAddrColIndex = ColumnNames.IndexOf("mem_addr");
        int memValColIndex = ColumnNames.IndexOf("mem_val");
        int isMemOpColIndex = ColumnNames.IndexOf("is_mem_op");
        int isMemWriteColIndex = ColumnNames.IndexOf("is_mem_write");
        int globalStepCounterColIndex = ColumnNames.IndexOf("global_step_counter");

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

        // Private input: trace_{i}_is_mem_op, trace_{i}_is_mem_write, trace_{i}_mem_addr, trace_{i}_mem_val, trace_{i}_global_step_counter
        // row -> column -> wire
        List<IReadOnlyList<Wire>> inTraceValueWires = [];
        for (int row = 0; row < traceCount; row++) {
            List<Wire> inTraceValues = [];
            for (int col = 0; col < ColumnNames.Count; col++) {
                Wire inTraceValueWire = Wire.NewPrivateInputWire($"in_trace_{row}_{ColumnNames[col]}");
                circuitBoard.AddWire(inTraceValueWire);
                inTraceValues.Add(inTraceValueWire);
            }
            inTraceValueWires.Add(inTraceValues);
        }

        // Compute hash for each trace row
        Wire constHashMemTraceMagicWire = Wire.NewConstantWire(MagicNumber.HashMemTraceMagic, "const_hash_mem_trace_magic");
        circuitBoard.AddWire(constHashMemTraceMagicWire);

        for (int row = 0; row < traceCount; row++) {
            // Public output: out_{i}_hash_mem_trace = H(magic, global_step_counter, trace_is_mem_op, trace_is_mem_write, trace_mem_addr, trace_mem_val, enc_key)
            Wire outHashMemTraceWire;
            {
                List<Wire> preimages = [
                    constHashMemTraceMagicWire,
                    inTraceValueWires[row][globalStepCounterColIndex],
                    inTraceValueWires[row][isMemOpColIndex],
                    inTraceValueWires[row][isMemWriteColIndex],
                    inTraceValueWires[row][memAddrColIndex],
                    inTraceValueWires[row][memValColIndex],
                    inEncKeyWire,
                ];
                GadgetInstance ins = MimcHashGadget.GetGadgetWithDefaultParams(preimages.Count).ApplyGadget(preimages, $"out_{row}_hash_mem_trace()");
                ins.Save(circuitBoard);
                outHashMemTraceWire = ins.OutputWires[0];
                outHashMemTraceWire.Name = $"out_{row}_hash_mem_trace";
                outHashMemTraceWire.IsPublicOutput = true;
            }
        }

        // Prepare last_access_i
        List<Wire> lastAccessWires = [];
        List<Wire> notLastAccessWires = [];
        for (int row = 0; row < traceCount - 1; row++) {
            int nextRow = row + 1;

            // last_access_i is 1 if mem_addr is not same as next row's, else 0
            Wire notLastAccessWire;
            {
                GadgetInstance ins = new FieldEqualsGadget().ApplyGadget([inTraceValueWires[row][memAddrColIndex], inTraceValueWires[nextRow][memAddrColIndex]], $"not_last_access_{row}()");
                ins.Save(circuitBoard);
                notLastAccessWire = ins.OutputWires[0];
                notLastAccessWire.Name = $"not_last_access_{row}";
            }

            Wire lastAccessWire;
            {
                GadgetInstance ins = new BoolNotGadget().ApplyGadget([notLastAccessWire], $"last_access_{row}()");
                ins.Save(circuitBoard);
                lastAccessWire = ins.OutputWires[0];
                lastAccessWire.Name = $"last_access_{row}";
            }

            lastAccessWires.Add(lastAccessWire);
            notLastAccessWires.Add(notLastAccessWire);
        }

        // Verify memory trace
        List<Wire> isSatisfiedWires = [];
        for (int row = 1; row < traceCount; row++) {
            int lastRow = row - 1;
            Wire lastAccessWire = lastAccessWires[lastRow];
            Wire notLastAccessWire = notLastAccessWires[lastRow];

            // We don't need to ensure is_mem_op and is_mem_write are in range [0, 1] -- the hash output above already does that

            // is_step_increased_i is 1 if global_step_counter is greater than last row's, else 0
            Wire isStepIncreasedWire;
            {
                GadgetInstance ins = new FieldLessThanGadget().ApplyGadget([inTraceValueWires[lastRow][globalStepCounterColIndex], inTraceValueWires[row][globalStepCounterColIndex]], $"is_step_increased_{row}()");
                ins.Save(circuitBoard);
                isStepIncreasedWire = ins.OutputWires[0];
                isStepIncreasedWire.Name = $"is_step_increased_{row}";
            }

            // is_addr_increased_i is 1 if mem_addr is greater than last row's, else 0
            Wire isAddrIncreasedWire;
            {
                GadgetInstance ins = new FieldLessThanGadget().ApplyGadget([inTraceValueWires[lastRow][memAddrColIndex], inTraceValueWires[row][memAddrColIndex]], $"is_addr_increased_{row}()");
                ins.Save(circuitBoard);
                isAddrIncreasedWire = ins.OutputWires[0];
                isAddrIncreasedWire.Name = $"is_addr_increased_{row}";
            }

            // if last_access_i is 1, we check is_addr_increased_i, otherwise we check is_step_increased_i. Should be 1.
            // (last_access and is_addr_increased) or (not last_access and is_step_increased)
            Wire isOrderSatisfiedWire;
            {
                Wire isLastAccessAddrIncreasedWire;
                {
                    GadgetInstance ins = new BoolAndGadget().ApplyGadget([lastAccessWire, isAddrIncreasedWire], $"is_last_access_addr_increased_{row}()");
                    ins.Save(circuitBoard);
                    isLastAccessAddrIncreasedWire = ins.OutputWires[0];
                    isLastAccessAddrIncreasedWire.Name = $"is_last_access_addr_increased_{row}";
                }

                Wire isNotLastAccessStepIncreasedWire;
                {
                    GadgetInstance ins = new BoolAndGadget().ApplyGadget([notLastAccessWire, isStepIncreasedWire], $"is_not_last_access_step_increased_{row}()");
                    ins.Save(circuitBoard);
                    isNotLastAccessStepIncreasedWire = ins.OutputWires[0];
                    isNotLastAccessStepIncreasedWire.Name = $"is_not_last_access_step_increased_{row}";
                }

                {
                    // We know that these two wires are always exclusive, so we can use a cheaper ADD gadget instead of an OR gadget. The result is still in range [0, 1]
                    GadgetInstance ins = new FieldAddGadget().ApplyGadget([isLastAccessAddrIncreasedWire, isNotLastAccessStepIncreasedWire], $"is_order_satisfied_{row}()");
                    ins.Save(circuitBoard);
                    isOrderSatisfiedWire = ins.OutputWires[0];
                    isOrderSatisfiedWire.Name = $"is_order_satisfied_{row}";
                }
            }

            // ZkVmExecutor already ensures that if an operation is not memory related (Store or Load), the mem_addr will be NegOne and the value will be Zero
            // Therefore, we can safely ignore is_mem_op

            // if is_mem_write_i is 0, and it's not last_access, we check that the value is not altered
            Wire isMemValIntegritySatisfiedWire;
            {
                Wire isIntegrityCheckIgnoredWire; // = is_mem_write_i OR last_access_i
                {
                    GadgetInstance ins = new BoolOrGadget().ApplyGadget([inTraceValueWires[row][isMemWriteColIndex], lastAccessWire], $"is_integrity_check_ignored_{row}()");
                    ins.Save(circuitBoard);
                    isIntegrityCheckIgnoredWire = ins.OutputWires[0];
                    isIntegrityCheckIgnoredWire.Name = $"is_integrity_check_ignored_{row}";
                }

                Wire isMemValEqualWire;
                {
                    GadgetInstance ins = new FieldEqualsGadget().ApplyGadget([inTraceValueWires[row][memValColIndex], inTraceValueWires[lastRow][memValColIndex]], $"is_mem_val_equal_{row}()");
                    ins.Save(circuitBoard);
                    isMemValEqualWire = ins.OutputWires[0];
                    isMemValEqualWire.Name = $"is_mem_val_equal_{row}";
                }

                // is_integrity_check_ignored_i OR is_mem_val_equal_i
                {
                    GadgetInstance ins = new BoolOrGadget().ApplyGadget([isIntegrityCheckIgnoredWire, isMemValEqualWire], $"is_mem_val_integrity_satisfied_{row}()");
                    ins.Save(circuitBoard);
                    isMemValIntegritySatisfiedWire = ins.OutputWires[0];
                    isMemValIntegritySatisfiedWire.Name = $"is_mem_val_integrity_satisfied_{row}";
                }
            }

            Wire isSatisfiedWire; // is_satisfied_i = is_order_satisfied_i AND is_mem_val_integrity_satisfied_i
            {
                GadgetInstance ins = new BoolAndGadget().ApplyGadget([isOrderSatisfiedWire, isMemValIntegritySatisfiedWire], $"is_satisfied_{row}()");
                ins.Save(circuitBoard);
                isSatisfiedWire = ins.OutputWires[0];
                isSatisfiedWire.Name = $"is_satisfied_{row}";
            }
            isSatisfiedWires.Add(isSatisfiedWire);
        }

        // Public output: out_is_satisfied = AND(is_satisfied_i)
        {
            GadgetInstance ins = new BoolAndGadget(isSatisfiedWires.Count).ApplyGadget(isSatisfiedWires, "out_is_satisfied()");
            ins.Save(circuitBoard);
            Wire outIsSatisfiedWire = ins.OutputWires[0];
            outIsSatisfiedWire.Name = "out_is_satisfied";
            outIsSatisfiedWire.IsPublicOutput = true;
        }

        return circuitBoard;
    }
}