using Anonymous.CompatCircuitProgramming.CircuitElements;
using Anonymous.CompatCircuitProgramming.Gadgets;

namespace Anonymous.CollaborativeZkVmExperiment.ExperimentOneCircuits;
public class SquaringCircuitBoardGenerator : ICircuitBoardGenerator {
    public int RepeatCount { get; set; } = 1024;

    public CircuitBoard GetCircuitBoard() {
        CircuitBoard circuitBoard = new();

        Wire inputWire = Wire.NewPrivateInputWire($"input");
        circuitBoard.AddWire(inputWire);

        Wire constOneWire = Wire.NewConstantWire(1); // note: zkSNARKs requires at least one public input
        circuitBoard.AddWire(constOneWire);

        Wire wire;
        {
            GadgetInstance ins = new FieldMulGadget().ApplyGadget([inputWire, constOneWire], $"input_times_one()");
            ins.Save(circuitBoard);
            wire = ins.OutputWires[0];
        }

        for (int i = 0; i < this.RepeatCount; i++) {
            GadgetInstance ins = new FieldMulGadget().ApplyGadget([wire, wire], $"mul_{i}()");
            ins.Save(circuitBoard);
            wire = ins.OutputWires[0];
        }

        Wire outputWire = wire;
        outputWire.Name = "output";
        outputWire.IsPublicOutput = true;

        return circuitBoard;

    }
}
