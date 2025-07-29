using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitProgramming.CircuitElements;
using Anonymous.CompatCircuitProgramming.Gadgets;
using System.Diagnostics;

namespace Anonymous.CollaborativeZkVm.ZkVmCircuits;
public class MemoryTraceFetchCircuitBoardGenerator(int traceCount) : ICircuitBoardGenerator {
    public int TraceCount { get; } = traceCount > 0 ? traceCount : throw new ArgumentOutOfRangeException(nameof(traceCount), "must be a positive integer");
    public static IReadOnlyList<string> ColumnNames { get; } = ["mem_addr", "global_step_counter", "is_mem_op", "is_mem_write", "mem_val"];
    public static IReadOnlyList<string> ColumnNamesToBeFetched { get; } = ["mem_val"];

    public CircuitBoard GetCircuitBoard() {
        int traceCount = this.TraceCount;

        CircuitBoard circuitBoard = new();

        // Private input: trace_{i}_is_mem_op, trace_{i}_is_mem_write, trace_{i}_mem_addr, trace_{i}_mem_val, trace_{i}_global_step_counter
        // column -> row -> wire (not to be confused!)
        List<IReadOnlyList<Wire>> inTraceValueWires = [];
        for (int col = 0; col < ColumnNames.Count; col++) {
            List<Wire> inTraceValues = [];

            for (int row = 0; row < traceCount; row++) {
                Wire inTraceValueWire = Wire.NewPrivateInputWire($"in_trace_{row}_{ColumnNames[col]}");
                circuitBoard.AddWire(inTraceValueWire);
                inTraceValues.Add(inTraceValueWire);
            }

            inTraceValueWires.Add(inTraceValues);
        }

        // Shortcut to mem_addr
        IReadOnlyList<Wire> inTraceMemAddrWires;
        {
            int memAddrColIndex = ColumnNames.IndexOf("mem_addr");
            Trace.Assert(ColumnNames[memAddrColIndex] == "mem_addr");

            inTraceMemAddrWires = inTraceValueWires[memAddrColIndex];
        }

        // Private input: in_mem_addr
        Wire inMemAddrWire = Wire.NewPrivateInputWire("in_mem_addr");
        circuitBoard.AddWire(inMemAddrWire);

        // Compare each row
        List<Wire> rowSelectionRawWires = []; // Note: rowSelectionRawWires is in reversed order

        for (int row = traceCount - 1; row >= 0; row--) {
            // row_selection_i_raw = 1 if mem_addr matches, 0 otherwise
            Wire rowSelectionRawWire;
            {
                GadgetInstance ins = new FieldEqualsGadget().ApplyGadget([inTraceMemAddrWires[row], inMemAddrWire], $"row_selection_{row}_raw()");
                ins.Save(circuitBoard);
                rowSelectionRawWire = ins.OutputWires[0];
                rowSelectionRawWire.Name = $"row_selection_{row}_raw";
            }

            rowSelectionRawWires.Add(rowSelectionRawWire); // Note: rowSelectionRawWires is in reversed order
        }

        // Compute "low bit" of raw selection bits
        List<Wire> rowSelectionLowBitWires;
        {
            GadgetInstance ins = new LowBitGadget(rowSelectionRawWires.Count).ApplyGadget(rowSelectionRawWires, "lowbit()");
            ins.Save(circuitBoard);
            rowSelectionLowBitWires = ins.OutputWires.ToList();
        }

        // The list is reversed to match the asc order
        rowSelectionLowBitWires.Reverse();

        Trace.Assert(rowSelectionLowBitWires.Count == traceCount);

        // Compose results
        foreach (string colName in ColumnNamesToBeFetched) {
            int col = ColumnNames.IndexOf(colName);
            Wire outTraceValueWire;
            {
                GadgetInstance ins = new SelectComposeGadget(traceCount).ApplyGadget([.. rowSelectionLowBitWires, .. inTraceValueWires[col]], $"out_trace_{ColumnNames[col]}()");
                ins.Save(circuitBoard);
                outTraceValueWire = ins.OutputWires[0];
                outTraceValueWire.Name = $"out_trace_{ColumnNames[col]}";
                outTraceValueWire.IsPrivateOutput = true;
            }
        }

        return circuitBoard;
    }
}
