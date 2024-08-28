using SadPencil.CompatCircuitProgramming.CircuitElements;
using SadPencil.CompatCircuitProgramming.Gadgets;

namespace SadPencil.CollaborativeZkVmExperiment.ExperimentOneCircuits;
public class BitDecompositionCircuitBoardGenerator : ICircuitBoardGenerator {
    public static int RepeatCount { get; } = 100;
    public CircuitBoard GetCircuitBoard() {
        CircuitBoard circuitBoard = new();

        Wire inputWire = Wire.NewPrivateInputWire($"input");
        circuitBoard.AddWire(inputWire);

        List<Wire> inputWiresDuplicated = [inputWire];
        for (int i = 1; i < RepeatCount; i++) {
            GadgetInstance ins = new FieldAddGadget().ApplyGadget([inputWiresDuplicated[i - 1], inputWiresDuplicated[i - 1]], $"add_{i}()");
            ins.Save(circuitBoard);
            inputWiresDuplicated.Add(ins.OutputWires[0]);
        }

        for (int i = 0; i < RepeatCount; i++) {
            GadgetInstance ins = new BitDecompositionGadget().ApplyGadget([inputWiresDuplicated[i]], $"bits_{i}()");
            ins.Save(circuitBoard);

            if (i == RepeatCount - 1) {
                for (int j = 0; j < ins.OutputWires.Count; j++) {
                    ins.OutputWires[j].IsPrivateOutput = true;
                }
            }
        }

        return circuitBoard;
    }
}
