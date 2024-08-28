using SadPencil.CompatCircuitProgramming.CircuitElements;
using SadPencil.CompatCircuitProgramming.Gadgets;

namespace SadPencil.CollaborativeZkVm.ZkVmCircuits;
public class PrivateInputShareCircuirBoardGenerator(int privateInputCount) : ICircuitBoardGenerator {
    public int PrivateInputCount { get; } = privateInputCount;
    public CircuitBoard GetCircuitBoard() {
        CircuitBoard circuitBoard = new();
        int privateInputCount = this.PrivateInputCount;

        Wire constNumberOneWire = Wire.NewConstantWire(1);
        circuitBoard.AddWire(constNumberOneWire);

        for (int i = 0; i < privateInputCount; i++) {
            Wire inputWire = Wire.NewPrivateInputWire($"input_{i}");
            circuitBoard.AddWire(inputWire);

            GadgetInstance ins = new FieldMulGadget().ApplyGadget([inputWire, constNumberOneWire], $"output_{i}()");
            ins.Save(circuitBoard);
            Wire outputWire = ins.OutputWires[0];
            outputWire.Name = $"output_{i}";
            outputWire.IsPrivateOutput = true;
        }

        return circuitBoard;
    }
}
