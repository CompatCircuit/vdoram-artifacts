using SadPencil.CompatCircuitCore.CompatCircuits;
using SadPencil.CompatCircuitCore.CompatCircuits.BasicCircuits;
using SadPencil.CompatCircuitCore.Extensions;
using SadPencil.CompatCircuitCore.GlobalConfig;
using SadPencil.CompatCircuitCore.SerilogHelpers;
using SadPencil.CompatCircuitProgramming.BitDecompositionProofCircuit;
using SadPencil.CompatCircuitProgramming.CircuitElements;
using Serilog;
using System.CommandLine;
using System.Diagnostics;
using Startup = SadPencil.CompatCircuitCore.Startup;

// _ = Trace.Listeners.Add(new ConsoleTraceListener(true));
_ = Trace.Listeners.Add(new SerilogTraceListener.SerilogTraceListener());

static Command GenerateBitDecompositionProofCircuitCommand() {
    Option<DirectoryInfo> outputFolderOption = new(name: "--output-folder", description: "Where to save the generated circuit files") { IsRequired = true };

    Command command = new("generate-bit-decomposition-proof-circuit", "Generate the bit decomposition proof circuit") { outputFolderOption };

    static void Handle(DirectoryInfo outputFolder) {
        // Note: BitDecompositionProofCircuit must only contains ADD and MUL operations, and all public outputs must be zero if the inputs are correct
        // Please manually follow this restriction. Will not check for this.
        // TODO: write unit tests for this restriction
        CircuitBoard circuitBoard = new BitDecompositionProofCircuitBoardGenerator().GetCircuitBoard().Optimize();
        CircuitBoardConverter.ToCompatCircuit(circuitBoard, $"BitDecompositionProofCircuit", out CompatCircuit compatCircuit, out CompatCircuitSymbols compatCircuitSymbols);
        BasicCircuit basicCircuit = new(compatCircuit);

        using (Stream stream = File.Open(Path.Combine(outputFolder.FullName, $"BitDecompositionProofCircuit.circuit.bin"), FileMode.Create, FileAccess.Write)) {
            byte[] buffer = new byte[basicCircuit.GetEncodedByteCount()];
            basicCircuit.EncodeBytes(buffer, out int _);
            stream.Write(buffer);
        }

        using (Stream stream = File.Open(Path.Combine(outputFolder.FullName, $"BitDecompositionProofCircuit.circuit"), FileMode.Create, FileAccess.Write)) {
            CompatCircuitSerializer.Serialize(basicCircuit, stream);
        }

        using (Stream stream = File.Open(Path.Combine(outputFolder.FullName, $"BitDecompositionProofCircuit.symbols.json"), FileMode.Create, FileAccess.Write)) {
            JsonSerializerHelper.Serialize(stream, compatCircuitSymbols, JsonConfig.JsonSerializerOptions);
        }
    }

    command.SetHandler(Handle, outputFolderOption);
    return command;
}

Serilog.Log.Logger = new Serilog.LoggerConfiguration()
    .MinimumLevel.ControlledBy(SerilogHelper.LoggingLevelSwitch)
    .WriteTo.Console(outputTemplate: SerilogHelper.OutputTemplate)
    .CreateLogger();

try {
    Startup.InitializeJsonSerializer();
    RootCommand rootCommand = new("CompatCircuitProgramming Program") {
        GenerateBitDecompositionProofCircuitCommand(),
    };

    return await rootCommand.InvokeAsync(args);
}
catch (Exception ex) {
    Serilog.Log.Error(ex.Message, ex);
    Debugger.Break();
    return 1;
}
finally {
    await Serilog.Log.CloseAndFlushAsync();
}
