using Anonymous.CollaborativeZkVm.ZkVmCircuits;
using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitProgramming.CircuitElements;
using System.Diagnostics;

namespace Anonymous.CollaborativeZkVmTest;
[TestClass]
public class MemoryTraceSortCircuitBoardGeneratorTest {
    private static void TimeIt(string actionName, Action action) {
        Stopwatch stopwatch = Stopwatch.StartNew();
        action();
        Serilog.Log.Information($"Action: {actionName}, Time: {stopwatch.Elapsed.TotalMilliseconds} ms");
    }
    private static T TimeIt<T>(string actionName, Func<T> func) {
        Stopwatch stopwatch = Stopwatch.StartNew();
        T ret = func();
        Serilog.Log.Information($"Func: {actionName}, Time: {stopwatch.Elapsed.TotalMilliseconds} ms");
        return ret;
    }

    private void VerifyMemoryTraceSortResult(IReadOnlyList<IReadOnlyList<Field>> traces) {
        IReadOnlyList<string> columnNames = MemoryTraceSortCircuitBoardGenerator.ColumnNames;
        int addrColIndex = columnNames.IndexOf("mem_addr");
        int globalStepCounterColIndex = columnNames.IndexOf("global_step_counter");

        int traceCount = traces.Count;

        // Verify the result
        for (int row = 1; row < traceCount; row++) {
            int lastRow = row - 1;

            Field lastAddr = traces[lastRow][addrColIndex];
            Field lastGlobalStepCounter = traces[lastRow][globalStepCounterColIndex];
            Field addr = traces[row][addrColIndex];
            Field globalStepCounter = traces[row][globalStepCounterColIndex];

            bool isLessEqualThan = lastAddr.Value < addr.Value || (addr.Value == lastAddr.Value && lastGlobalStepCounter.Value <= globalStepCounter.Value);
            Assert.IsTrue(isLessEqualThan);
        }
    }

    private IReadOnlyList<IReadOnlyList<Field>> TestMemoryTraceSortCircuit(IReadOnlyList<IReadOnlyList<Field>> traces) {
        IReadOnlyList<string> columnNames = MemoryTraceSortCircuitBoardGenerator.ColumnNames;
        int addrColIndex = columnNames.IndexOf("mem_addr");
        int globalStepCounterColIndex = columnNames.IndexOf("global_step_counter");

        if (traces.Any(row => row.Count != columnNames.Count)) {
            throw new ArgumentException("Unexpected trace width", nameof(traces));
        }

        int count = traces.Count;

        // Compile SortCircuit
        CircuitBoard sortCircuit = TimeIt($"Gen SortCircuit-n{count}", () => new MemoryTraceSortCircuitBoardGenerator(count).GetCircuitBoard());

        sortCircuit = TimeIt($"Optimize SortCircuit-n{count}", () => sortCircuit.Optimize());

        Serilog.Log.Information($"SortCircuit.Wires: {sortCircuit.WireCount}");
        Serilog.Log.Information($"SortCircuit.Operations: {sortCircuit.OperationCount}");

        // Prepare single executor
        CircuitBoardSingleExecutorWrapper singleExecutorWrapper = CircuitBoardSingleExecutorWrapper.FromNewSingleExecutor(sortCircuit, $"MemoryTraceSortCircuit-{count}");

        for (int col = 0; col < columnNames.Count; col++) {
            string colName = columnNames[col];
            for (int row = 0; row < count; row++) {
                singleExecutorWrapper.AddPrivate($"in_trace_{row}_{colName}", traces[row][col]);
            }
        }

        // Compute
        _ = TimeIt($"Compute SortCircuit-{count}", singleExecutorWrapper.Compute);

        // Receive the result
        {
            List<IReadOnlyList<Field>> resultTraces = [];
            for (int row = 0; row < count; row++) {
                List<Field> traceRow = [];
                for (int col = 0; col < columnNames.Count; col++) {
                    traceRow.Add(singleExecutorWrapper.GetOutput($"out_trace_{row}_{columnNames[col]}"));
                }
                resultTraces.Add(traceRow);

                // Update traces
                traces = resultTraces;
            }
        }

        // Verify the result
        this.VerifyMemoryTraceSortResult(traces);

        return traces;
    }

    private IReadOnlyList<IReadOnlyList<Field>> TestMemoryTraceSortInnerLoopCircuit(IReadOnlyList<IReadOnlyList<Field>> traces) {
        IReadOnlyList<string> columnNames = MemoryTraceSortCircuitBoardGenerator.ColumnNames;
        int addrColIndex = columnNames.IndexOf("mem_addr");
        int globalStepCounterColIndex = columnNames.IndexOf("global_step_counter");

        if (traces.Any(row => row.Count != columnNames.Count)) {
            throw new ArgumentException("Unexpected trace width", nameof(traces));
        }

        int traceCount = traces.Count;

        foreach ((int k, int j) in MemoryTraceSortInnerLoopCircuitBoardGenerator.GetAllLoopIndexKJ(traceCount)) {
            CircuitBoard sortCircuit = TimeIt($"Gen SortCircuit-n{traceCount}-k{k}-j{j}", () => new MemoryTraceSortInnerLoopCircuitBoardGenerator(traceCount, k, j).GetCircuitBoard());
            sortCircuit = TimeIt($"Optimize SortCircuit-n{traceCount}-k{k}-j{j}", () => sortCircuit.Optimize());

            Serilog.Log.Information($"SortCircuit.Wires: {sortCircuit.WireCount}");
            Serilog.Log.Information($"SortCircuit.Operations: {sortCircuit.OperationCount}");

            // Prepare single executor
            CircuitBoardSingleExecutorWrapper singleExecutorWrapper = CircuitBoardSingleExecutorWrapper.FromNewSingleExecutor(sortCircuit, $"MemoryTraceSortCircuit-n{traceCount}-k{k}-j{j}");

            for (int col = 0; col < columnNames.Count; col++) {
                string colName = columnNames[col];
                for (int row = 0; row < traceCount; row++) {
                    singleExecutorWrapper.AddPrivate($"in_trace_{row}_{colName}", traces[row][col]);
                }
            }

            // Compute
            _ = TimeIt($"Compute SortCircuit-n{traceCount}-k{k}-j{j}", singleExecutorWrapper.Compute);

            // Receive the result
            List<IReadOnlyList<Field>> resultTraces = [];
            for (int row = 0; row < traceCount; row++) {
                List<Field> traceRow = [];
                for (int col = 0; col < columnNames.Count; col++) {
                    traceRow.Add(singleExecutorWrapper.GetOutput($"out_trace_{row}_{columnNames[col]}"));
                }
                resultTraces.Add(traceRow);
            }

            // Update traces
            traces = resultTraces;
        }

        this.VerifyMemoryTraceSortResult(traces);

        return traces;
    }

    [TestMethod]
    [DataRow(2, true, true)]
    [DataRow(4, true, true)]
    [DataRow(8, true, true)]
    [DataRow(16, true, true)]
    [DataRow(32, false, true)]
    public void TestMemoryTraceSortCircuitBoardGenerator(int traceCount, bool testRegularSort, bool testInnerLoopSort) {
        if (!testInnerLoopSort && !testRegularSort) {
            throw new Exception("At least one test should be specified");
        }

        int count = traceCount;

        CircuitBoard sortCircuit = TimeIt($"Gen SortCircuit-n{count}", () => new MemoryTraceSortCircuitBoardGenerator(count).GetCircuitBoard());

        sortCircuit = TimeIt($"Optimize SortCircuit-n{count}", () => sortCircuit.Optimize());

        Serilog.Log.Information($"SortCircuit.Wires: {sortCircuit.WireCount}");
        Serilog.Log.Information($"SortCircuit.Operations: {sortCircuit.OperationCount}");

        // Test whether the sorting implementation works as expected

        // Prepare input
        IReadOnlyList<string> columnNames = MemoryTraceSortCircuitBoardGenerator.ColumnNames;

        List<Field> testElements = [];
        const int testCaseSize = 6;
        for (int i = 0; i < (count * columnNames.Count) + testCaseSize; i += testCaseSize) {
            Field rand = ArithConfig.FieldFactory.Random();
            int testElementCount = testElements.Count;

            testElements.Add(rand);
            testElements.Add(rand - ArithConfig.FieldFactory.One);
            testElements.Add(rand + ArithConfig.FieldFactory.One);
            testElements.Add(ArithConfig.FieldFactory.One);
            testElements.Add(ArithConfig.FieldFactory.Zero);
            testElements.Add(ArithConfig.FieldFactory.NegOne);

            Assert.AreEqual(testElementCount + testCaseSize, testElements.Count);
        }
        Assert.IsTrue(testElements.Count >= count * columnNames.Count);

        IEnumerator<Field> testElementEnumerator = testElements.GetEnumerator();

        // row -> column -> value
        List<IReadOnlyList<Field>> traces = [];
        for (int row = 0; row < count; row++) {
            List<Field> traceRow = [];
            for (int col = 0; col < columnNames.Count; col++) {
                bool available = testElementEnumerator.MoveNext();
                Assert.IsTrue(available);
                traceRow.Add(testElementEnumerator.Current);
            }
            traces.Add(traceRow);
        }

        if (testInnerLoopSort && testRegularSort) {
            IReadOnlyList<IReadOnlyList<Field>> outTraces1 = this.TestMemoryTraceSortInnerLoopCircuit(traces);
            IReadOnlyList<IReadOnlyList<Field>> outTraces2 = this.TestMemoryTraceSortCircuit(traces);
            Assert.AreEqual(traces.Count, outTraces1.Count);
            Assert.AreEqual(traces.Count, outTraces2.Count);
            for (int i = 0; i < outTraces1.Count; i++) {
                Assert.IsTrue(outTraces1[i].SequenceEqual(outTraces2[i]));
            }
        }
        else {
            if (testInnerLoopSort) {
                _ = this.TestMemoryTraceSortInnerLoopCircuit(traces);
            }

            if (testRegularSort) {
                _ = this.TestMemoryTraceSortCircuit(traces);
            }
        }

    }

}