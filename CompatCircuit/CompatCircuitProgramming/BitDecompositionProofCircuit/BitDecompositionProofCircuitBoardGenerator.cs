using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitProgramming.CircuitElements;
using Anonymous.CompatCircuitProgramming.Gadgets;
using System.Numerics;

namespace Anonymous.CompatCircuitProgramming.BitDecompositionProofCircuit;
public class BitDecompositionProofCircuitBoardGenerator : ICircuitBoardGenerator {
    public CircuitBoard GetCircuitBoard() {
        int bitSize = ArithConfig.BitSize;

        CircuitBoard circuitBoard = new();

        Wire constOneWire = Wire.NewConstantWire(1);
        circuitBoard.AddWire(constOneWire);

        IReadOnlyDictionary<int, Wire> twoPowerWires;
        {
            Dictionary<int, Wire> twoPowerWireDict = [];
            twoPowerWireDict.Add(0, constOneWire);

            BigInteger twoPowers = 1;
            for (int exponent = 1; exponent < bitSize; exponent++) {
                twoPowers *= 2;
                Wire powerWire = Wire.NewConstantWire(twoPowers);
                circuitBoard.AddWire(powerWire);
                twoPowerWireDict.Add(exponent, powerWire);
            }

            twoPowerWires = twoPowerWireDict;
        }

        Wire constZeroWire = Wire.NewConstantWire(ArithConfig.FieldFactory.Zero);
        circuitBoard.AddWire(constZeroWire);

        Wire constNegOneWire = Wire.NewConstantWire(ArithConfig.FieldFactory.NegOne);
        circuitBoard.AddWire(constNegOneWire);

        Wire inputFieldWire = Wire.NewPrivateInputWire("input_field");
        circuitBoard.AddWire(inputFieldWire);

        List<Wire> inputBitWires = [];
        for (int i = 0; i < bitSize; i++) {
            Wire inputWire = Wire.NewPrivateInputWire($"input_bit_{i}");
            circuitBoard.AddWire(inputWire);

            inputBitWires.Add(inputWire);
        }

        // Public output: out_error_if_not_equals. Should be 0 if and only if the input matches. Otherwise the value might be anything other than 0.
        Wire outErrorIfNotEqualsWire;
        {
            // Compute \Sum_{i=0}^{bitSize-1} inputBitWires[i] * (2^i)
            List<Wire> inputTimes2PowersWires = [];
            for (int i = 0; i < bitSize; i++) {
                Wire inputTimes2PowerWire;
                {
                    GadgetInstance ins = new FieldMulGadget().ApplyGadget([inputBitWires[i], twoPowerWires[i]], $"{i}_times_2_power_{i}");
                    ins.Save(circuitBoard);
                    inputTimes2PowerWire = ins.OutputWires[0];
                    inputTimes2PowerWire.Name = $"{i}_times_2_power_{i}";
                }
                inputTimes2PowersWires.Add(inputTimes2PowerWire);
            }

            Wire inputTimes2PowersSumWire;
            {
                GadgetInstance ins = new FieldAddGadget(inputTimes2PowersWires.Count).ApplyGadget(inputTimes2PowersWires, "input_times_2_powers_sum()");
                ins.Save(circuitBoard);
                inputTimes2PowersSumWire = ins.OutputWires[0];
                inputTimes2PowersSumWire.Name = "input_times_2_powers_sum";
            }

            // out_error_if_not_equals = inputField - inputTimes2PowersSum
            {
                GadgetInstance ins = new FieldSubGadget().ApplyGadget([inputFieldWire, inputTimes2PowersSumWire], "out_error_if_not_equals()");
                ins.Save(circuitBoard);
                outErrorIfNotEqualsWire = ins.OutputWires[0];
                outErrorIfNotEqualsWire.Name = "out_error_if_not_equals";
                outErrorIfNotEqualsWire.IsPublicOutput = true;
            }
        }

        // Public output: out_error_if_input_bit_out_of_range. Should be 0 if and only if all input bits are either 0 or 1. Otherwise the value might be anything other than 0.
        Wire outErrorIfInputBitOutOfRangeWire;
        {
            List<Wire> inputMinusOneWires = [];
            for (int i = 0; i < bitSize; i++) {
                Wire inputMinusOneWire;
                {
                    GadgetInstance ins = new FieldAddGadget().ApplyGadget([inputBitWires[i], constNegOneWire], $"{i}_minus_one()");
                    ins.Save(circuitBoard);
                    inputMinusOneWire = ins.OutputWires[0];
                    inputMinusOneWire.Name = $"{i}_minus_one";
                }
                inputMinusOneWires.Add(inputMinusOneWire);
            }

            // Compute x * (x - 1). If x is 0 or 1, the result should be 0.
            List<Wire> xTimesXMinusOneWires = [];
            for (int i = 0; i < bitSize; i++) {
                Wire xTimesXMinusOneWire;
                {
                    GadgetInstance ins = new FieldMulGadget().ApplyGadget([inputBitWires[i], inputMinusOneWires[i]], $"{i}_times_{i}_minus_one()");
                    ins.Save(circuitBoard);
                    xTimesXMinusOneWire = ins.OutputWires[0];
                    xTimesXMinusOneWire.Name = $"{i}_times_{i}_minus_one";
                }
                xTimesXMinusOneWires.Add(xTimesXMinusOneWire);
            }

            // Sum up. The result should be 0 if and only if x is 0 or 1.
            {
                GadgetInstance ins = new FieldAddGadget(xTimesXMinusOneWires.Count).ApplyGadget(xTimesXMinusOneWires, "out_error_if_input_bit_out_of_range()");
                ins.Save(circuitBoard);
                outErrorIfInputBitOutOfRangeWire = ins.OutputWires[0];
                outErrorIfInputBitOutOfRangeWire.Name = "out_error_if_input_bit_out_of_range";
                outErrorIfInputBitOutOfRangeWire.IsPublicOutput = true;
            }
        }

        // Public output: out_error_if_overflow. Should be 0 if and only if the number that input bits represent is within the range of [0, p). Otherwise the value might be anything other than 0.
        Wire outErrorIfOverflowWire;
        {
            List<Wire> fieldSizeBits = [];
            {
                foreach (bool bit in ArithConfig.FieldFactory.FieldSize.BitDecompositionUnsigned()) {
                    Wire fieldSizeBitWire = bit ? constOneWire : constZeroWire;
                    fieldSizeBits.Add(fieldSizeBitWire);
                }
            }

            Wire areInputBitsLessThanFieldSizeWire;
            {
                GadgetInstance ins = new BitsLessThanGadget(bitSize).ApplyGadget([.. inputBitWires, .. fieldSizeBits], "are_input_bits_less_than_field_size()");
                ins.Save(circuitBoard);
                areInputBitsLessThanFieldSizeWire = ins.OutputWires[0];
                areInputBitsLessThanFieldSizeWire.Name = "are_input_bits_less_than_field_size";
            }

            {
                GadgetInstance ins = new BoolNotGadget().ApplyGadget([areInputBitsLessThanFieldSizeWire], "out_error_if_overflow()");
                ins.Save(circuitBoard);
                outErrorIfOverflowWire = ins.OutputWires[0];
                outErrorIfOverflowWire.Name = "out_error_if_overflow";
                outErrorIfOverflowWire.IsPublicOutput = true;
            }
        }

        // Note: BitDecompositionProofCircuit must only contain ADD and MUL operations!!
        return circuitBoard.Operations.Any(op => op.OperationType is not OperationType.Addition and not OperationType.Multiplication)
            ? throw new Exception("The circuit contains operations other than ADD and MUL")
            : circuitBoard;
    }
}
