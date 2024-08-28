using SadPencil.CompatCircuitProgramming.CircuitElements;
using SadPencil.CompatCircuitProgramming.Gadgets;

namespace SadPencil.CollaborativeZkVmExperiment.ExperimentOneCircuits;
public class MultiplicationCircuitBoardGenerator : ICircuitBoardGenerator {
    public static int RepeatCount { get; } = 100000;
    public CircuitBoard GetCircuitBoard() {
        CircuitBoard circuitBoard = new();

        Wire inputWire = Wire.NewPrivateInputWire($"input");
        circuitBoard.AddWire(inputWire);

        Wire constOneWire = Wire.NewConstantWire(1); // note: zkSNARKs requires at least one public input
        circuitBoard.AddWire(constOneWire);

        Wire wireA = constOneWire;
        Wire wireB = inputWire;

        for (int i = 0; i < RepeatCount; i++) {
            GadgetInstance ins = new FieldMulGadget().ApplyGadget([wireA, wireB], $"mul_{i}()");
            ins.Save(circuitBoard);
            wireA = wireB;
            wireB = ins.OutputWires[0];
        }

        Wire outputWire = wireB;
        outputWire.Name = "output";
        outputWire.IsPrivateOutput = true;

        return circuitBoard;
    }
}
