using Anonymous.CollaborativeZkVm.ZkVmCircuits;
using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CollaborativeZkVmTest;
[TestClass]
public class ZkVmCircuitTest {
    [TestMethod]
    [DataRow(16)]
    [DataRow(32)]
    [DataRow(64)]
    [DataRow(128)]
    public void TestCompileZkVmCircuit(int regCount) {
        Serilog.Log.Information($"regCount: {regCount}");
        CircuitBoard board = new ZkVmExecutorCircuitBoardGenerator(regCount).GetCircuitBoard().Optimize();
        Serilog.Log.Information($"board.Wires: {board.WireCount}");
        Serilog.Log.Information($"board.Operations: {board.OperationCount}");
    }
}
