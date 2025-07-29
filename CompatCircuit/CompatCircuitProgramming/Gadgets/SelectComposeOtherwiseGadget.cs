using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CompatCircuitProgramming.Gadgets;
public class SelectComposeOtherwiseGadget(int selectionCount) : IGadget {
    public int SelectionCount { get; } = selectionCount > 0 ? selectionCount : throw new ArgumentOutOfRangeException(nameof(selectionCount), "must be a positive integer");
    public List<string> GetInputWireNames() => [
        .. Enumerable.Range(0, this.SelectionCount).Select(i => $"condition_{i}"),
        .. Enumerable.Range(0, this.SelectionCount).Select(i => $"value_{i}"),
        "value_otherwise",
    ];
    public List<string> GetOutputWireNames() => Enumerable.Range(0, this.SelectionCount).Select(i => $"selected_value").ToList();

    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != (2 * this.SelectionCount) + 1) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        List<Wire> conditions = [];
        List<Wire> values = [];
        for (int i = 0; i < this.SelectionCount; i++) {
            Wire selectionWire = inputWires[i];
            Wire valueWire = inputWires[i + this.SelectionCount];

            conditions.Add(selectionWire);
            values.Add(valueWire);
        }

        // Choices are always exclusive. So, to deal with "otherwise" case, we need to first sum up all choices above (the result should be either 0 or 1) and then negate it.
        Wire otherwiseConditionWire;
        {
            Wire conditionSumWire;
            {
                if (this.SelectionCount == 1) {
                    conditionSumWire = conditions[0];
                }
                else {
                    GadgetInstance ins = new FieldAddGadget(this.SelectionCount).ApplyGadget(conditions, $"{namePrefix}_[select_compose_otherwise]_sum()");
                    ins.Save(circuitBoard);
                    conditionSumWire = ins.OutputWires[0];
                }
            }

            {
                GadgetInstance ins = new BoolNotGadget().ApplyGadget([conditionSumWire], $"{namePrefix}_[select_compose_otherwise]_not()");
                ins.Save(circuitBoard);
                otherwiseConditionWire = ins.OutputWires[0];
            }
        }

        conditions.Add(otherwiseConditionWire);
        values.Add(inputWires[2 * this.SelectionCount]);

        Wire resultWire;
        {
            GadgetInstance ins = new SelectComposeGadget(this.SelectionCount + 1).ApplyGadget([.. conditions, .. values], $"{namePrefix}_[select_compose_otherwise]_select()");
            ins.Save(circuitBoard);
            resultWire = ins.OutputWires[0];
        }

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard([resultWire], circuitBoard);
    }
}
