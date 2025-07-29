using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CompatCircuitProgramming.Gadgets;
public class FieldConstPowGadget(Field exponent) : IGadget {
    public Field Exponent { get; } = exponent;
    public List<string> GetInputWireNames() => ["base"];
    public List<string> GetOutputWireNames() => ["power"];
    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != 1) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        // Note: to compute x^y
        // result = 1
        // baseToCurrentPower = x
        // foreach yBit in yBits {
        //   if yBit {
        //     result = result * baseToCurrentPower
        //   }
        //   baseToCurrentPower = baseToCurrentPower * baseToCurrentPower
        // }

        Wire oneWire = Wire.NewConstantWire(ArithConfig.FieldFactory.One, $"{namePrefix}_[field_compare]_one");
        circuitBoard.AddNewConstantWire(oneWire);

        Wire result = oneWire;
        Wire baseToCurrentPower = inputWires[0];
        IReadOnlyList<bool> exponentBits = this.Exponent.Value.BitDecompositionUnsigned();
        for (int i = 0; i < exponentBits.Count; i++) {
            if (exponentBits[i]) {
                // result = result * baseToCurrentPower
                GadgetInstance ins = new FieldMulGadget().ApplyGadget([result, baseToCurrentPower], $"{namePrefix}_[field_compare]_pow_true_{i}_mul()");
                ins.Save(circuitBoard);
                result = ins.OutputWires[0];
            }
            // baseToCurrentPower = baseToCurrentPower * baseToCurrentPower
            {
                GadgetInstance ins = new FieldMulGadget().ApplyGadget([baseToCurrentPower, baseToCurrentPower], $"{namePrefix}_[field_compare]_pow_base_{i}_mul()");
                ins.Save(circuitBoard);
                baseToCurrentPower = ins.OutputWires[0];
            }
        }

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard([result], circuitBoard);
    }
}
