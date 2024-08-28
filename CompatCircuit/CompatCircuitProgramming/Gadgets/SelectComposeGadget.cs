using SadPencil.CompatCircuitProgramming.CircuitElements;
using System.Diagnostics;

namespace SadPencil.CompatCircuitProgramming.Gadgets;
public class SelectComposeGadget(int selectionCount) : IGadget {
    public int SelectionCount { get; } = selectionCount > 0 ? selectionCount : throw new ArgumentOutOfRangeException(nameof(selectionCount), "must be a positive integer");
    public List<string> GetInputWireNames() => [
        .. Enumerable.Range(0, this.SelectionCount).Select(i => $"condition_{i}"),
        .. Enumerable.Range(0, this.SelectionCount).Select(i => $"value_{i}"),
    ];
    public List<string> GetOutputWireNames() => Enumerable.Range(0, this.SelectionCount).Select(i => $"selected_value").ToList();

    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != 2 * this.SelectionCount) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        Wire? finalSelectionValue = null;
        for (int i = 0; i < this.SelectionCount; i++) {
            Wire selectionWire = inputWires[i];
            Wire valueWire = inputWires[i + this.SelectionCount];

            GadgetInstance ins1 = new FieldMulGadget().ApplyGadget([selectionWire, valueWire], $"{namePrefix}_[select_compose]_{i}_prod()");
            ins1.Save(circuitBoard);

            if (finalSelectionValue is null) {
                finalSelectionValue = ins1.OutputWires[0];
            }
            else {
                GadgetInstance ins2 = new FieldAddGadget().ApplyGadget([finalSelectionValue, ins1.OutputWires[0]], $"{namePrefix}_[select_compose]_{i}_sum()");
                ins2.Save(circuitBoard);
                finalSelectionValue = ins2.OutputWires[0];
            }
        }

        Trace.Assert(this.SelectionCount > 0 && finalSelectionValue is not null);

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard([finalSelectionValue!], circuitBoard);
    }
}
