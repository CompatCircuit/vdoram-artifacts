using SadPencil.CompatCircuitProgramming.CircuitElements;
using SadPencil.CompatCircuitProgramming.Gadgets;

namespace SadPencil.CollaborativeZkVmExperiment.ExperimentOneCircuits;
public class InversionCircuitBoardGenerator {
    public static int RepeatCount { get; } = 1000;
    public CircuitBoard GetCircuitBoard() {
        CircuitBoard circuitBoard = new();

        Wire inputWire = Wire.NewPrivateInputWire($"input");
        circuitBoard.AddWire(inputWire);

        for (int i = 0; i < RepeatCount; i++) {
            GadgetInstance ins = new FieldInverseGadget().ApplyGadget([inputWire], $"inv_{i}()");
            ins.Save(circuitBoard);

            inputWire = ins.OutputWires[0];

            if (i == RepeatCount - 1) {
                Wire outputWire = ins.OutputWires[0];
                outputWire.Name = "output";
                outputWire.IsPrivateOutput = true;
            }
        }

        return circuitBoard;
    }
}
