using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitProgramming.CircuitElements;
using Anonymous.CompatCircuitProgramming.Gadgets;
using System.Diagnostics;
using System.Numerics;

namespace Anonymous.CollaborativeZkVm.ZkVmCircuits;
public class MemoryTraceSortInnerLoopCircuitBoardGenerator : ICircuitBoardGenerator {
    public static IReadOnlyList<string> ColumnNames => MemoryTraceFetchCircuitBoardGenerator.ColumnNames;
    public int TraceCount { get; }
    public int LoopIndexK { get; }
    public int LoopIndexJ { get; }

    public static IEnumerable<(int k, int j)> GetAllLoopIndexKJ(int traceCount) {
        int n = traceCount;
        for (int k = 2; k <= n; k *= 2) {
            for (int j = k / 2; j > 0; j /= 2) {
                yield return (k, j);
            }
        }
    }

    public MemoryTraceSortInnerLoopCircuitBoardGenerator(int traceCount, int loopIndexK, int loopIndexJ) {
        if (!BitOperations.IsPow2(traceCount)) {
            throw new ArgumentOutOfRangeException(nameof(traceCount), "must be a power of 2");
        }

        if (traceCount < 2) {
            throw new ArgumentOutOfRangeException(nameof(traceCount), "must be at least 2");
        }

        this.TraceCount = traceCount;
        this.LoopIndexK = loopIndexK;
        this.LoopIndexJ = loopIndexJ;
    }

    public CircuitBoard GetCircuitBoard() {
        int memAddrColIndex = ColumnNames.IndexOf("mem_addr");
        int globalStepCounterColIndex = ColumnNames.IndexOf("global_step_counter");

        CircuitBoard circuitBoard = new();

        // Private input: trace_{i}_is_mem_op, trace_{i}_is_mem_write, trace_{i}_mem_addr, trace_{i}_mem_val, trace_{i}_global_step_counter
        // row -> column -> wire
        List<IReadOnlyList<Wire>> inTraceValueWires = [];
        for (int row = 0; row < this.TraceCount; row++) {
            List<Wire> inTraceValues = [];
            for (int col = 0; col < ColumnNames.Count; col++) {
                Wire inTraceValueWire = Wire.NewPrivateInputWire($"in_trace_{row}_{ColumnNames[col]}");
                circuitBoard.AddWire(inTraceValueWire);
                inTraceValues.Add(inTraceValueWire);
            }
            inTraceValueWires.Add(inTraceValues);
        }

        List<IReadOnlyList<Wire>> traceValueWires = inTraceValueWires.ToList();

        void SwapIf(int x, int y, bool swapIfGreater, string namePrefix) {
            Wire xAddrWire = traceValueWires[x][memAddrColIndex];
            Wire yAddrWire = traceValueWires[y][memAddrColIndex];
            Wire xGlobalStepCounterWire = traceValueWires[x][globalStepCounterColIndex];
            Wire yGlobalStepCounterWire = traceValueWires[y][globalStepCounterColIndex];

            // Note: Bitonic sorter is NOT stable
            // So we need to define "less than" of two rows: x.addr < y.addr || (x.addr == y.addr && x.global_step_counter < y.global_step_counter)
            // Similarly, "greater than": x.addr > y.addr || (x.addr == y.addr && x.global_step_counter > y.global_step_counter)

            Wire swapConditionWire;
            {
                Wire addrSwapConditionWire; // x.addr < y.addr (or x.addr > y.addr if swapIfGreater)
                {
                    List<Wire> inputWires = swapIfGreater ? [yAddrWire, xAddrWire] : [xAddrWire, yAddrWire];
                    GadgetInstance ins = new FieldLessThanGadget().ApplyGadget(inputWires, $"{namePrefix}_addr_swap_condition()");
                    ins.Save(circuitBoard);
                    addrSwapConditionWire = ins.OutputWires[0];
                    addrSwapConditionWire.Name = $"{namePrefix}_addr_swap_condition";
                }

                Wire addrEqualsWire; // x.addr == y.addr
                {
                    GadgetInstance ins = new FieldEqualsGadget().ApplyGadget([xAddrWire, yAddrWire], $"{namePrefix}_addr_equals()");
                    ins.Save(circuitBoard);
                    addrEqualsWire = ins.OutputWires[0];
                    addrEqualsWire.Name = $"{namePrefix}_addr_equals";
                }

                Wire globalStepCounterSwapConditionWire; // x.global_step_counter < y.global_step_counter (or x.global_step_counter > y.global_step_counter if swapIfGreater)
                {
                    List<Wire> inputWires = swapIfGreater ? [yGlobalStepCounterWire, xGlobalStepCounterWire] : [xGlobalStepCounterWire, yGlobalStepCounterWire];
                    GadgetInstance ins = new FieldLessThanGadget().ApplyGadget(inputWires, $"{namePrefix}_global_step_counter_swap_condition()");
                    ins.Save(circuitBoard);
                    globalStepCounterSwapConditionWire = ins.OutputWires[0];
                    globalStepCounterSwapConditionWire.Name = $"{namePrefix}_global_step_counter_swap_condition";
                }

                Wire primarySwapCondition = addrSwapConditionWire;

                Wire secondarySwapCondition; // = addrEqualsWire && globalStepCounterSwapConditionWire
                {
                    GadgetInstance ins = new BoolAndGadget().ApplyGadget([addrEqualsWire, globalStepCounterSwapConditionWire], $"{namePrefix}_secondary_swap_condition()");
                    ins.Save(circuitBoard);
                    secondarySwapCondition = ins.OutputWires[0];
                    secondarySwapCondition.Name = $"{namePrefix}_secondary_swap_condition";
                }

                // swapCondition = primarySwapCondition || secondarySwapCondition
                {
                    GadgetInstance ins = new BoolOrGadget().ApplyGadget([primarySwapCondition, secondarySwapCondition], $"{namePrefix}_swap_condition()");
                    ins.Save(circuitBoard);
                    swapConditionWire = ins.OutputWires[0];
                    swapConditionWire.Name = $"{namePrefix}_swap_condition";
                }
            }

            Wire notSwapConditionWire;
            {
                GadgetInstance ins = new BoolNotGadget().ApplyGadget([swapConditionWire], $"{namePrefix}_not_swap_condition()");
                ins.Save(circuitBoard);
                notSwapConditionWire = ins.OutputWires[0];
                notSwapConditionWire.Name = $"{namePrefix}_not_swap_condition";
            }

            List<IReadOnlyList<Wire>> newWireRows = [];

            foreach (bool isX in new bool[] { true, false }) {
                int index = isX ? x : y;
                int otherIndex = isX ? y : x;
                string isXStr = isX ? "x" : "y";

                List<Wire> newWireRow = [];
                for (int col = 0; col < ColumnNames.Count; col++) {
                    Wire newWireValueWire;
                    {
                        GadgetInstance ins = new SelectComposeGadget(2).ApplyGadget([
                            swapConditionWire,
                            notSwapConditionWire,
                            traceValueWires[otherIndex][col],
                            traceValueWires[index][col]],
                            $"{namePrefix}_{isXStr}_new_{ColumnNames[col]}()");
                        ins.Save(circuitBoard);
                        newWireValueWire = ins.OutputWires[0];
                        newWireValueWire.Name = $"{namePrefix}_{isXStr}_new_{ColumnNames[col]}";
                    }

                    newWireRow.Add(newWireValueWire);
                }

                newWireRows.Add(newWireRow);
            }

            traceValueWires[x] = newWireRows[0];
            traceValueWires[y] = newWireRows[1];
        }

        int n = this.TraceCount;
        int k = this.LoopIndexK;
        int j = this.LoopIndexJ;

        for (int i = 0; i < n; i++) {
            int iXj = i ^ j; // bitwize XOR
            if (iXj > i) {
                if ((i & k) == 0) {
                    // Swap if greater
                    SwapIf(i, iXj, swapIfGreater: true, $"swap_{k}_{j}_{i}");
                }
                else {
                    // Swap if less
                    SwapIf(i, iXj, swapIfGreater: false, $"swap_{k}_{j}_{i}");
                }
            }
        }

        for (int row = 0; row < this.TraceCount; row++) {
            for (int col = 0; col < ColumnNames.Count; col++) {
                Wire outTraceValueWire = traceValueWires[row][col];
                Trace.Assert(outTraceValueWire != inTraceValueWires[row][col]);
                outTraceValueWire.Name = $"out_trace_{row}_{ColumnNames[col]}";
                outTraceValueWire.IsPrivateOutput = true;
            }
        }

        return circuitBoard;
    }
}
