using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitCore.GlobalConfig;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.CompatCircuits;
public static class CompatCircuitSerializer {
    public static CompatCircuit Deserialize(Stream stream, int bufferSize = -1, bool leaveOpen = false) {
        // Read from a UTF-8 text file
        using StreamReader reader = StreamIOHelper.NewUtf8StreamReader(stream, bufferSize, leaveOpen: true);

        int reservedWireConstantCount = CompatCircuit.ReservedWireConstantCount;

        // int.MinValue should never be used; assigned to make compiler happy
        int constantWireCount = int.MinValue;
        int publicInputWireCount = int.MinValue;
        int inputWireCount = int.MinValue;
        int wireCount = int.MinValue;

        List<Field?> constantInputs = [];
        HashSet<int> publicOutputs = [];
        List<CompatCircuitOperation?> operations = [];
        List<bool> operationResultDefined = [];

        bool isPreamble = true;
        string preambleCommandState = "INIT"; // INIT, RESERVED, CONST, PUBIN, PRIVIN, TOTAL

        // Read each line
        while (true) {
            string? line = reader.ReadLine();
            if (line is null) {
                break;
            }

            line = line.Trim();

            // Ignore comments after semi-colon character
            int commentIndex = line.IndexOf(';');
            if (commentIndex >= 0) {
                line = line[..commentIndex];
            }

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line)) {
                continue;
            }

            // Split the line by space
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Parse the first word
            string command = parts[0].ToUpperInvariant();

            // Preamble lines
            if (isPreamble) {
                // Example:
                // RESERVED 0 .. 256
                // CONST 257 .. 258 (or CONST NONE)
                // PUBIN 259 .. 260 (or PUBIN NONE)
                // PRIVIN 261 .. 262 (or PRIVIN NONE)
                // TOTAL 521

                switch (command) {
                    case "RESERVED":
                    case "CONST":
                    case "PUBIN":
                    case "PRIVIN":
                        // TODO: refactor codes. Too ugly.
                        if (parts.Length == 4) {
                            // Parse range
                            if (parts[2] != "..") {
                                throw new Exception($"Invalid preamble line ({command}): should have '..' in the middle");
                            }

                            int starting = int.Parse(parts[1]);
                            int ending = int.Parse(parts[3]);

                            if (ending < starting) {
                                throw new Exception($"Invalid preamble line ({command}): ending ID should be greater or equal than starting ID");
                            }

                            // Set values
                            switch (command) {
                                case "RESERVED":
                                    if (preambleCommandState != "INIT") {
                                        throw new Exception("Invalid preamble line (RESERVED): must be placed at the beginning");
                                    }

                                    if (starting != 0 || ending != reservedWireConstantCount - 1) {
                                        throw new Exception($"Invalid preamble line (RESERVED): range should be 0 .. {reservedWireConstantCount - 1}");
                                    }

                                    preambleCommandState = "RESERVED";
                                    break;
                                case "CONST":
                                    if (preambleCommandState != "RESERVED") {
                                        throw new Exception("Invalid preamble line (CONST): must be placed after RESERVED");
                                    }

                                    if (starting != reservedWireConstantCount) {
                                        throw new Exception($"Invalid preamble line (CONST): range should start from {reservedWireConstantCount}");
                                    }

                                    constantWireCount = ending + 1;

                                    Trace.Assert(constantInputs.Count == 0);
                                    constantInputs.AddRange(Enumerable.Repeat<Field?>(null, ending - starting + 1));

                                    preambleCommandState = "CONST";
                                    break;
                                case "PUBIN":
                                    if (preambleCommandState != "CONST") {
                                        throw new Exception("Invalid preamble line (PUBIN): must be placed after CONST");
                                    }

                                    if (starting != constantWireCount) {
                                        throw new Exception($"Invalid preamble line (PUBIN): range should start from {constantWireCount}");
                                    }

                                    publicInputWireCount = ending + 1;

                                    preambleCommandState = "PUBIN";
                                    break;
                                case "PRIVIN":
                                    if (preambleCommandState != "PUBIN") {
                                        throw new Exception("Invalid preamble line (PRIVIN): must be placed after PUBIN");
                                    }

                                    if (starting != publicInputWireCount) {
                                        throw new Exception($"Invalid preamble line (PRIVIN): range should start from {publicInputWireCount}");
                                    }

                                    inputWireCount = ending + 1;

                                    preambleCommandState = "PRIVIN";
                                    break;
                                default:
                                    throw new Exception("Assert failed. Unknown preamble command.");
                            }
                        }
                        else if (parts.Length == 2) {
                            if (command == "RESERVED") {
                                throw new Exception($"Invalid preamble line ({command})");
                            }
                            if (parts[1].ToUpperInvariant() != "NONE") {
                                throw new Exception($"Invalid preamble line ({command})");
                            }

                            switch (command) {
                                case "CONST":
                                    if (preambleCommandState != "RESERVED") {
                                        throw new Exception("Invalid preamble line (CONST): must be placed after RESERVED");
                                    }

                                    constantWireCount = reservedWireConstantCount;
                                    preambleCommandState = "CONST";
                                    break;
                                case "PUBIN":
                                    if (preambleCommandState != "CONST") {
                                        throw new Exception("Invalid preamble line (PUBIN): must be placed after CONST");
                                    }

                                    publicInputWireCount = constantWireCount;
                                    preambleCommandState = "PUBIN";
                                    break;
                                case "PRIVIN":
                                    if (preambleCommandState != "PUBIN") {
                                        throw new Exception("Invalid preamble line (PRIVIN): must be placed after PUBIN");
                                    }

                                    inputWireCount = publicInputWireCount;
                                    preambleCommandState = "PRIVIN";
                                    break;
                                default:
                                    throw new Exception("Assert failed. Unknown preamble command.");
                            }
                        }
                        else {
                            throw new Exception($"Invalid preamble line ({command})");
                        }

                        break;

                    case "TOTAL":
                        if (preambleCommandState != "PRIVIN") {
                            throw new Exception("Invalid preamble line (TOTAL): must be placed after PRIVIN");
                        }

                        if (parts.Length != 2) {
                            throw new Exception("Invalid preamble line (TOTAL)");
                        }

                        wireCount = int.Parse(parts[1]);

                        if (wireCount <= inputWireCount) {
                            throw new Exception("Invalid preamble line (TOTAL): total wire count is too small");
                        }

                        Trace.Assert(operationResultDefined.Count == 0);
                        operationResultDefined.AddRange(Enumerable.Repeat(false, wireCount - inputWireCount));

                        preambleCommandState = "TOTAL";
                        break;

                    default:
                        throw new Exception($"Invalid preamble line: unknown command '{command}'");
                }

                // Check if all preamble commands are satisfied
                if (preambleCommandState == "TOTAL") {
                    isPreamble = false;
                }
                continue;
            }

            // Non-Preamble lines
            // Example:
            // CONST 257 = 114514; assign constant 114514 to wire 257
            // ADD 261 = 257 + 259 + 260; define wire 261 as the sum of wire 257, 259, and 260
            // MUL 262 = 257 * 258 * 259; define wire 262 as the product of wire 257, 258, and 259
            // INV 263 FROM 261; define wire 263 as the inverse of wire 261
            // BITS 267 .. 521 FROM 265; define wire 267 to 521 as the bits of wire 265
            // OUTPUT 263; mark a single wire 263 as public output
            // OUTPUT 267 .. 521; mark wires 267 to 521 as public output

            switch (command) {
                case "CONST": {
                        if (parts.Length != 4 || parts[2] != "=") {
                            throw new Exception($"Invalid CONST line");
                        }

                        int wireID = int.Parse(parts[1]);
                        Field value = ArithConfig.FieldFactory.FromString(parts[3]);

                        // Check if the wire ID is in range [ReservedWireCount, ConstantWireCount)
                        if (wireID < reservedWireConstantCount || wireID >= constantWireCount) {
                            throw new Exception($"Invalid CONST line: wire ID {wireID} is out of range");
                        }

                        // Check if the wire ID is not assigned yet
                        if (constantInputs[wireID - reservedWireConstantCount] is not null) {
                            throw new Exception($"Invalid CONST line: wire ID {wireID} is already assigned");
                        }

                        // Write the value
                        constantInputs[wireID - reservedWireConstantCount] = value;
                    }

                    break;
                case "OUTPUT": {
                        if (parts.Length is not 2 and not 4) {
                            throw new Exception($"Invalid OUTPUT line");
                        }

                        if (parts.Length == 2) {
                            int wireID = int.Parse(parts[1]);

                            // Check if the wire ID is in range [InputWireCount, WireCount)
                            if (wireID < inputWireCount || wireID >= wireCount) {
                                throw new Exception($"Invalid OUTPUT line: wire ID {wireID} is out of range");
                            }

                            // Mark the wire as public output
                            bool nonExisted = publicOutputs.Add(wireID);
                            if (!nonExisted) {
                                throw new Exception($"Invalid OUTPUT line: wire ID {wireID} is already marked as public output");
                            }
                        }
                        else {
                            int starting = int.Parse(parts[1]);
                            int ending = int.Parse(parts[3]);

                            // Check if the wire IDs are in range [InputWireCount, WireCount)
                            if (starting < inputWireCount || ending >= wireCount) {
                                throw new Exception($"Invalid OUTPUT line: wire IDs between {starting} and {ending} are out of range");
                            }

                            // Mark the wires as public output
                            for (int wireID = starting; wireID <= ending; wireID++) {
                                bool nonExisted = publicOutputs.Add(wireID);
                                if (!nonExisted) {
                                    throw new Exception($"Invalid OUTPUT line: wire ID {wireID} is already marked as public output");
                                }
                            }
                        }
                    }
                    break;
                case "ADD":
                case "MUL":
                case "INV":
                case "BITS": {
                        switch (command) {
                            case "ADD":
                            case "MUL": {
                                    if (parts.Length < 4 || parts.Length % 2 != 0 || parts[2] != "=") {
                                        throw new Exception($"Invalid {command} line");
                                    }

                                    Trace.Assert(command is "ADD" or "MUL");
                                    string op = command == "ADD" ? "+" : "*";

                                    for (int i = 4; i < parts.Length; i += 2) {
                                        if (parts[i] != op) {
                                            throw new Exception($"Invalid {command} line: expected '{op}'");
                                        }
                                    }

                                    // Get output wire ID
                                    int outputWireID = int.Parse(parts[1]);

                                    // Get input wire IDs
                                    List<int> inputWireIDs = [];
                                    for (int i = 3; i < parts.Length; i += 2) {
                                        inputWireIDs.Add(int.Parse(parts[i]));
                                    }

                                    // Check if output wire ID is assigned
                                    if (operationResultDefined[outputWireID - inputWireCount]) {
                                        throw new Exception($"Invalid {command} line: output wire ID {outputWireID} is already assigned");
                                    }
                                    operationResultDefined[outputWireID - inputWireCount] = true;

                                    // Save the operation
                                    Trace.Assert(command is "ADD" or "MUL");
                                    CompatCircuitOperation operation = new(
                                        command == "ADD" ? CompatCircuitOperationType.Addition : CompatCircuitOperationType.Multiplication,
                                        inputWireIDs,
                                        [outputWireID]);
                                    operations.Add(operation);
                                }

                                break;
                            case "INV": {
                                    if (parts.Length != 4 || parts[2].ToUpperInvariant() != "FROM") {
                                        throw new Exception($"Invalid INV line");
                                    }

                                    // Get output wire ID
                                    int outputWireID = int.Parse(parts[1]);

                                    // Get input wire ID
                                    int inputWireID = int.Parse(parts[3]);

                                    // Check if output wire ID is assigned
                                    if (operationResultDefined[outputWireID - inputWireCount]) {
                                        throw new Exception($"Invalid {command} line: output wire ID {outputWireID} is already assigned");
                                    }
                                    operationResultDefined[outputWireID - inputWireCount] = true;

                                    // Save the operation
                                    CompatCircuitOperation operation = new(CompatCircuitOperationType.Inversion, [inputWireID], [outputWireID]);
                                    operations.Add(operation);

                                }
                                break;
                            case "BITS": {
                                    if (parts.Length != 6 || parts[2] != ".." || parts[4].ToUpperInvariant() != "FROM") {
                                        throw new Exception($"Invalid BITS line");
                                    }

                                    // Get output wire IDs
                                    int starting = int.Parse(parts[1]);
                                    int ending = int.Parse(parts[3]);

                                    // Get input wire ID
                                    int inputWireID = int.Parse(parts[5]);

                                    // Check if output wire IDs are assigned
                                    for (int i = starting; i <= ending; i++) {
                                        if (operationResultDefined[i - inputWireCount]) {
                                            throw new Exception($"Invalid BITS line: output wire ID {i} is already assigned");
                                        }
                                        operationResultDefined[i - inputWireCount] = true;
                                    }

                                    // Save the operation
                                    CompatCircuitOperation operation = new(CompatCircuitOperationType.BitDecomposition, [inputWireID], Enumerable.Range(starting, ending - starting + 1).ToList());
                                    operations.Add(operation);
                                }
                                break;
                        }

                    }
                    break;
                default:
                    throw new Exception($"Invalid command '{command}'");
            }

        }

        // Finally, make sure all constants, public inputs, private inputs are defined, and all operations are assigned
        // Throw an exception showing the mismatched wire IDs
        for (int i = 0; i < constantInputs.Count; i++) {
            if (constantInputs[i] is null) {
                throw new Exception($"Constant wire ID {i + reservedWireConstantCount} is not defined");
            }
        }

        for (int i = 0; i < operationResultDefined.Count; i++) {
            if (!operationResultDefined[i]) {
                throw new Exception($"Wire ID {i + inputWireCount} is not defined");
            }
        }

        // Create the circuit
        return new CompatCircuit() {
            ConstantWireCount = constantWireCount,
            PublicInputWireCount = publicInputWireCount,
            InputWireCount = inputWireCount,
            WireCount = wireCount,
            ConstantInputs = constantInputs.Select(x => x!).ToList(),
            PublicOutputs = publicOutputs,
            Operations = operations.Select(x => x!).ToList(),
        };
    }

    public static void Serialize(CompatCircuit circuit, Stream stream, int bufferSize = -1, bool leaveOpen = false) {
        // Write to a UTF-8 text file
        using StreamWriter writer = StreamIOHelper.NewUtf8StreamWriter(stream, bufferSize, leaveOpen);

        // Write preamble
        writer.WriteLine($"RESERVED 0 .. {CompatCircuit.ReservedWireConstantCount - 1}");

        if (circuit.ConstantWireCount > CompatCircuit.ReservedWireConstantCount) {
            writer.WriteLine($"CONST {CompatCircuit.ReservedWireConstantCount} .. {circuit.ConstantWireCount - 1}");
        }
        else {
            writer.WriteLine($"CONST NONE");
        }

        if (circuit.PublicInputWireCount > circuit.ConstantWireCount) {
            writer.WriteLine($"PUBIN {circuit.ConstantWireCount} .. {circuit.PublicInputWireCount - 1}");
        }
        else {
            writer.WriteLine($"PUBIN NONE");
        }

        if (circuit.InputWireCount > circuit.PublicInputWireCount) {
            writer.WriteLine($"PRIVIN {circuit.PublicInputWireCount} .. {circuit.InputWireCount - 1}");
        }
        else {
            writer.WriteLine($"PRIVIN NONE");
        }

        writer.WriteLine($"TOTAL {circuit.WireCount}");

        // Write constant values
        for (int i = CompatCircuit.ReservedWireConstantCount; i < circuit.ConstantWireCount; i++) {
            writer.WriteLine($"CONST {i} = {circuit.ConstantInputs[i - CompatCircuit.ReservedWireConstantCount]}");
        }

        // Write operations
        foreach (CompatCircuitOperation operation in circuit.Operations) {
            switch (operation.OperationType) {
                case CompatCircuitOperationType.Addition:
                    writer.WriteLine($"ADD {operation.OutputWires[0]} = {string.Join(" + ", operation.InputWires)}");
                    break;
                case CompatCircuitOperationType.Multiplication:
                    writer.WriteLine($"MUL {operation.OutputWires[0]} = {string.Join(" * ", operation.InputWires)}");
                    break;
                case CompatCircuitOperationType.Inversion:
                    writer.WriteLine($"INV {operation.OutputWires[0]} FROM {operation.InputWires[0]}");
                    break;
                case CompatCircuitOperationType.BitDecomposition: {
                        // Check if output wire IDs are sequential (increasing by 1)
                        for (int i = 1; i < operation.OutputWires.Count; i++) {
                            if (operation.OutputWires[i] != operation.OutputWires[i - 1] + 1) {
                                throw new Exception("Bit decomposition output wire IDs should be sequential");
                            }
                        }

                        writer.WriteLine($"BITS {operation.OutputWires[0]} .. {operation.OutputWires[^1]} FROM {operation.InputWires[0]}");
                    }
                    break;
                default:
                    throw new Exception("Unrecognized operation type");
            }
        }

        // Write public outputs
        foreach (int wireID in circuit.PublicOutputs) {
            writer.WriteLine($"OUTPUT {wireID}");
        }
    }
}
