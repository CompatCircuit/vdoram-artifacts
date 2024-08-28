using SadPencil.CompatCircuitCore;
using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.CompatCircuits;
using SadPencil.CompatCircuitCore.CompatCircuits.MpcCircuits;
using SadPencil.CompatCircuitCore.Computation;
using SadPencil.CompatCircuitCore.Computation.MultiParty;
using SadPencil.CompatCircuitCore.Computation.MultiParty.Network;
using SadPencil.CompatCircuitCore.Computation.MultiParty.SharedStorages;
using SadPencil.CompatCircuitCore.Computation.SingleParty;
using SadPencil.CompatCircuitCore.Extensions;
using SadPencil.CompatCircuitCore.GlobalConfig;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.DaBitPrioPlus;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.EdaBitsKai;
using SadPencil.CompatCircuitCore.SerilogHelpers;
using Serilog;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Net;

// _ = Trace.Listeners.Add(new ConsoleTraceListener(true));
_ = Trace.Listeners.Add(new SerilogTraceListener.SerilogTraceListener());

Command GeneratePresharedCommand() {
    Option<DirectoryInfo> outputDirectoryOption = new(name: "--directory", description: "The directory where preshared files for each party are saved") { IsRequired = true };
    Option<int> partyCountOption = new("--parties", "The number of parties in secure multi-party computation") { IsRequired = true };
    Option<int> arithTripleCountOption = new("--field-beaver-triples", "The number of field beaver triples to be used. For example, 100000. Each beaver triple can only be used once, the larger, the better") { IsRequired = true };
    Option<int> boolTripleCountOption = new("--bool-beaver-triples", "The number of boolean beaver triples to be used. For example, 10000000. Each beaver triple can only be used once, the larger, the better") { IsRequired = true };
    Option<int> edaBitsPairCountOption = new("--edaBits-pair", "The number of edaBits pair to be used. For example, 1000. Each edaBits pair can only be used once, the larger, the better") { IsRequired = true };
    Option<int> daBitPrioPlusCountOption = new("--daBitPrioPlus-pair", "The number of daBitPrioPlus to be used. For example, 100000. Each daBitPrioPlus can only be used once, the larger, the better") { IsRequired = true };

    Command command = new("gen-preshared", "Generate preshared beaver triples, edaBits pairs, and daBitPrioPlus pairs") { outputDirectoryOption, partyCountOption, arithTripleCountOption, boolTripleCountOption, edaBitsPairCountOption, daBitPrioPlusCountOption };

    command.SetHandler((DirectoryInfo outputDirectoryInfo, int partyCount, int arithTripleCount, int boolTripleCount, int edaBitsPairCount, int daBitPrioPlusCount) => {
        // Generate FieldBeaverTripleShares
        {
            Serilog.Log.Information($"Generate {arithTripleCount} FieldBeaverTripleShares");
            string GetFileName(int i) => $"FieldBeaver.{i}.bin";
            List<Stream> streams = Enumerable.Range(0, partyCount).Select(GetFileName).Select(filename => File.Open(Path.Combine(outputDirectoryInfo.FullName, filename), FileMode.Create, FileAccess.Write) as Stream).ToList();
            FieldBeaverTripleGenerator fieldBeaverTripleGenerator = new() { FieldFactory = ArithConfig.FieldFactory, FieldSecretSharing = ArithConfig.FieldSecretSharing };
            fieldBeaverTripleGenerator.GenerateBeaverTripleShareFileForAllParties(streams, partyCount, arithTripleCount, leaveOpen: false);
        }

        // Generate BoolBeaverTripleShareLists
        {
            Serilog.Log.Information($"Generate {boolTripleCount} BoolBeaverTripleShares");
            string GetFileName(int i) => $"BoolBeaver.{i}.bin";
            List<Stream> streams = Enumerable.Range(0, partyCount).Select(GetFileName).Select(filename => File.Open(Path.Combine(outputDirectoryInfo.FullName, filename), FileMode.Create, FileAccess.Write) as Stream).ToList();
            BoolBeaverTripleGenerator boolBeaverTripleGenerator = new() { RandomGenerator = RandomConfig.RandomGenerator, BoolSecretSharing = ArithConfig.BoolSecretSharing };
            boolBeaverTripleGenerator.GenerateBeaverTripleShareFileForAllParties(streams, partyCount, boolTripleCount, leaveOpen: false);
        }

        // Generate edaBitsKaiShares
        {
            Serilog.Log.Information($"Generate {edaBitsPairCount} edaBitsKaiShares");
            string GetFileName(int i) => $"edaBits.{i}.bin";
            List<Stream> streams = Enumerable.Range(0, partyCount).Select(GetFileName).Select(filename => File.Open(Path.Combine(outputDirectoryInfo.FullName, filename), FileMode.Create, FileAccess.Write) as Stream).ToList();
            EdaBitsKaiGenerator edaBitsKaiGenerator = new(ArithConfig.BitSize, partyCount, ArithConfig.FieldFactory, ArithConfig.FieldSecretSharing, ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator);
            edaBitsKaiGenerator.GenerateEdaBitsShareFileForAllParties(streams, edaBitsPairCount, leaveOpen: false);
        }

        // Generate daBitPrioPlusShares
        {
            Serilog.Log.Information($"Generate {daBitPrioPlusCount} daBitPrioPlusShares");
            string GetFileName(int i) => $"daBitPrioPlus.{i}.bin";
            List<Stream> streams = Enumerable.Range(0, partyCount).Select(GetFileName).Select(filename => File.Open(Path.Combine(outputDirectoryInfo.FullName, filename), FileMode.Create, FileAccess.Write) as Stream).ToList();
            DaBitPrioPlusGenerator daBitPrioPlusGenerator = new() { FieldFactory = ArithConfig.FieldFactory, FieldSecretSharing = ArithConfig.FieldSecretSharing, BoolSecretSharing = ArithConfig.BoolSecretSharing, RandomGenerator = RandomConfig.RandomGenerator };
            daBitPrioPlusGenerator.GenerateDaBitPrioPlusShareFileForAllParties(streams, partyCount, daBitPrioPlusCount, leaveOpen: false);
        }
    }, outputDirectoryOption, partyCountOption, arithTripleCountOption, boolTripleCountOption, edaBitsPairCountOption, daBitPrioPlusCountOption);

    return command;
}

Command GenerateExampleConfigCommand() {
    Option<int> partyCountOption = new("--parties", "The number of parties in secure multi-party computation") { IsRequired = true };

    Command command = new("gen-config", "Generate example configuration file for Party 0") { partyCountOption };

    command.SetHandler((int partyCount) => {
        if (partyCount > 100) {
            throw new Exception("Party count is too large. Please manually write MPC configuration files for each party.");
        }

        List<IPAddress> partyIPAddresses = [];
        for (int i = 0; i < partyCount; i++) {
            partyIPAddresses.Add(IPAddress.Parse($"192.168.0.{100 + i}"));
        }
        MpcConfig config = new() {
            PartyIPAddresses = partyIPAddresses,
            MyID = 0,
            MyIPAddress = IPAddress.Parse($"0.0.0.0"),
        };

        // Hint: Keep this Console.WriteLine. It intended writes to stdout. Don't replace it with a Log method.
        Console.WriteLine(JsonSerializerHelper.Serialize(config, JsonConfig.JsonSerializerOptions));
    }, partyCountOption);

    return command;
}

Command RunSingleExecutorCommand() {
    Option<FileInfo> circuitFileOption = new(name: "--circuit", description: "The compat circuit file to be executed") { IsRequired = true };
    Option<FileInfo> publicInputFileOption = new(name: "--public-input", description: "File containing all public input values") { IsRequired = true };
    Option<FileInfo> privateInputFileOption = new(name: "--private-input", description: "File containing all private input values") { IsRequired = true };
    Option<FileInfo> outputFileOption = new(name: "--output", description: "Where the file containing all values to be saved") { IsRequired = true };

    Command command = new("run-single", "Compute a compat circuit without multi-party computation") { circuitFileOption, publicInputFileOption, privateInputFileOption, outputFileOption };

    void Handle(FileInfo circuitFile, FileInfo publicInputFile, FileInfo privateInputFile, FileInfo outputFile) {
        CompatCircuit circuit;
        using (Stream stream = File.Open(circuitFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            circuit = CompatCircuitSerializer.Deserialize(stream);
        }

        Dictionary<int, Field> publicInputValueDict;
        using (Stream stream = File.Open(publicInputFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            publicInputValueDict = JsonSerializerHelper.Deserialize<Dictionary<int, Field>>(stream, JsonConfig.JsonSerializerOptions) ?? throw new Exception("Cannot parse public input file");
        }

        Dictionary<int, Field> privateInputValueDict;
        using (Stream stream = File.Open(privateInputFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            privateInputValueDict = JsonSerializerHelper.Deserialize<Dictionary<int, Field>>(stream, JsonConfig.JsonSerializerOptions) ?? throw new Exception("Cannot parse private input file");
        }

        SingleExecutor executor = new();
        CircuitExecuteResult executeResult = executor.Compute(CompatCircuitConverter.ToMpcCircuit(circuit), publicInputValueDict, privateInputValueDict);

        using (Stream stream = File.Open(outputFile.FullName, FileMode.Create, FileAccess.Write)) {
            JsonSerializerHelper.Serialize(stream, executeResult.PublicOutputs, JsonConfig.JsonSerializerOptions);
        }
    }

    command.SetHandler(Handle, circuitFileOption, publicInputFileOption, privateInputFileOption, outputFileOption);
    return command;
}

Command RunMpcExecutorCommand() {
    Option<FileInfo> mpcConfigFileOption = new(name: "--config", description: "The MPC configuration file") { IsRequired = true };
    Option<FileInfo> circuitFileOption = new(name: "--circuit", description: "The compat circuit file to be executed") { IsRequired = true };
    Option<FileInfo> publicInputFileOption = new(name: "--public-input", description: "File containing all public input values") { IsRequired = true };
    Option<FileInfo> privateInputFileOption = new(name: "--private-input", description: "File containing private input values owned by this party") { IsRequired = true };
    Option<FileInfo> fieldBeaverFileOption = new(name: "--field-beaver", description: "File containing this party's secret shares of field beaver triples") { IsRequired = true };
    Option<FileInfo> boolBeaverFileOption = new(name: "--bool-beaver", description: "File containing this party's secret shares of boolean beaver triples") { IsRequired = true };
    Option<FileInfo> edaBitsFileOption = new(name: "--edaBits", description: "File containing this party's secret shares of edaBits pairs") { IsRequired = true };
    Option<FileInfo> daBitPrioPlusFileOption = new(name: "--daBitPrioPlus", description: "File containing this party's secret shares of daBitPrioPlus") { IsRequired = true };
    Option<FileInfo> publicOutputFileOption = new(name: "--public-output", description: "Where the file containing public output values to be saved") { IsRequired = true };
    Option<bool> repeatPresharedOption = new(name: "--unsafe-repeat-preshared", description: "Repeatly use preshared values. This is extremely unsafe and only meant for debugging or evaluation purpose.", getDefaultValue: () => false) { IsRequired = false };

    Command command = new("run-mpc", "Compute a compat circuit with multi-party computation") { mpcConfigFileOption, circuitFileOption, publicInputFileOption, privateInputFileOption, fieldBeaverFileOption, boolBeaverFileOption, edaBitsFileOption, daBitPrioPlusFileOption, publicOutputFileOption, repeatPresharedOption };

    async Task Handle(InvocationContext invocationContext) => await AsyncHelper.TerminateOnException(async () => {
        FileInfo mpcConfigFile = invocationContext.ParseResult.GetValueForOption(mpcConfigFileOption)!;
        FileInfo circuitFile = invocationContext.ParseResult.GetValueForOption(circuitFileOption)!;
        FileInfo publicInputFile = invocationContext.ParseResult.GetValueForOption(publicInputFileOption)!;
        FileInfo privateInputFile = invocationContext.ParseResult.GetValueForOption(privateInputFileOption)!;
        FileInfo fieldBeaverFile = invocationContext.ParseResult.GetValueForOption(fieldBeaverFileOption)!;
        FileInfo boolBeaverFile = invocationContext.ParseResult.GetValueForOption(boolBeaverFileOption)!;
        FileInfo edaBitsFile = invocationContext.ParseResult.GetValueForOption(edaBitsFileOption)!;
        FileInfo daBitPrioPlusFile = invocationContext.ParseResult.GetValueForOption(daBitPrioPlusFileOption)!;
        FileInfo publicOutputFile = invocationContext.ParseResult.GetValueForOption(publicOutputFileOption)!;
        bool repeatPreshared = invocationContext.ParseResult.GetValueForOption(repeatPresharedOption)!;

        MpcConfig mpcConfig;
        using (Stream stream = File.Open(mpcConfigFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            mpcConfig = JsonSerializerHelper.Deserialize<MpcConfig>(stream, JsonConfig.JsonSerializerOptions) ?? throw new Exception("Cannot parse MPC configuration file");
        }

        Serilog.Log.Information("Preparing...");
        ICountingEnumerator<FieldBeaverTripleShare> fieldBeaverTripleShareEnumerator;
        ICountingEnumerator<BoolBeaverTripleShare> boolBeaverTripleShareEnumerator;
        ICountingEnumerator<EdaBitsKaiShare> edaBitsKaiShareEnumerator;
        ICountingEnumerator<DaBitPrioPlusShare> daBitPrioPlusShareEnumerator;

        fieldBeaverTripleShareEnumerator = new FieldBeaverTripleShareFileEnumerator(File.Open(fieldBeaverFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read), ArithConfig.FieldFactory);
        boolBeaverTripleShareEnumerator = new BoolBeaverTripleShareFileEnumerator(File.Open(boolBeaverFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read));
        edaBitsKaiShareEnumerator = new EdaBitsKaiShareFileEnumerator(File.Open(edaBitsFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read), ArithConfig.FieldFactory);
        daBitPrioPlusShareEnumerator = new DaBitPrioPlusShareFileEnumerator(File.Open(daBitPrioPlusFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read), ArithConfig.FieldFactory);

        if (repeatPreshared) {
            fieldBeaverTripleShareEnumerator = new CountingEnumerator<FieldBeaverTripleShare>(new RepeatingEnumerator<FieldBeaverTripleShare>(fieldBeaverTripleShareEnumerator));
            boolBeaverTripleShareEnumerator = new CountingEnumerator<BoolBeaverTripleShare>(new RepeatingEnumerator<BoolBeaverTripleShare>(boolBeaverTripleShareEnumerator));
            edaBitsKaiShareEnumerator = new CountingEnumerator<EdaBitsKaiShare>(new RepeatingEnumerator<EdaBitsKaiShare>(edaBitsKaiShareEnumerator));
            daBitPrioPlusShareEnumerator = new CountingEnumerator<DaBitPrioPlusShare>(new RepeatingEnumerator<DaBitPrioPlusShare>(daBitPrioPlusShareEnumerator));
        }

        MpcCircuit circuit;
        using (Stream stream = File.Open(circuitFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            CompatCircuit compatCircuit = CompatCircuitSerializer.Deserialize(stream);
            circuit = CompatCircuitConverter.ToMpcCircuit(compatCircuit);
        }

        Dictionary<int, Field> publicInputValueDict;
        using (Stream stream = File.Open(publicInputFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            publicInputValueDict = JsonSerializerHelper.Deserialize<Dictionary<int, Field>>(stream, JsonConfig.JsonSerializerOptions) ?? throw new Exception("Cannot parse public input file");
        }

        Dictionary<int, Field> privateInputValueDict;
        using (Stream stream = File.Open(privateInputFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            privateInputValueDict = JsonSerializerHelper.Deserialize<Dictionary<int, Field>>(stream, JsonConfig.JsonSerializerOptions) ?? throw new Exception("Cannot parse private input file");
        }

        Serilog.Log.Information("Connecting to MPC network...");

        UdpMpcClient mpcClient = new(new NetMpcClientConfig(mpcConfig));
        MpcSharedStorageSessionManager manager = new(mpcClient);
        IMpcSharedStorage mpcSharedStorage = new MpcSharedStorage(manager, sessionID: 0, mpcConfig.PartyCount);

        mpcClient.Start();
        try {
            MpcExecutor executor = new(
                new MpcExecutorConfig(mpcConfig),
                mpcSharedStorage,
                fieldBeaverTripleShareEnumerator,
                boolBeaverTripleShareEnumerator,
                edaBitsKaiShareEnumerator,
                daBitPrioPlusShareEnumerator) { LoggerPrefix = $"MPC{mpcConfig.MyID}" };

            CircuitExecuteResult result = await executor.Compute(circuit, publicInputValueDict, privateInputValueDict);

            using Stream stream = File.Open(publicOutputFile.FullName, FileMode.Create, FileAccess.Write);
            JsonSerializerHelper.Serialize(stream, result.PublicOutputs, JsonConfig.JsonSerializerOptions);

            // Print time cost
            Serilog.Log.Information($"Total time cost: {result.TotalTime.TotalSeconds:F6} seconds");

            // Print how many shares are used
            Serilog.Log.Information($"FieldBeaverTripleShare used: {fieldBeaverTripleShareEnumerator.Count}");
            Serilog.Log.Information($"BoolBeaverTripleShare used: {boolBeaverTripleShareEnumerator.Count}");
            Serilog.Log.Information($"EdaBitsKaiShare used: {edaBitsKaiShareEnumerator.Count}");
            Serilog.Log.Information($"DaBitPrioPlusShare used: {daBitPrioPlusShareEnumerator.Count}");

            // Print network traffic usage
            Serilog.Log.Information($"Total sent: {mpcClient.TotalBytesSent} bytes");

        }
        catch (Exception ex) {
            Serilog.Log.Error(ex.Message, ex);
            Debugger.Break();
            throw;
        }
        finally {
            Serilog.Log.Information("Process completed.");

            mpcClient.Stop();

            Serilog.Log.Information("Shutdown.");
        }
    });

    command.SetHandler(Handle);
    return command;
}

Serilog.Log.Logger = new Serilog.LoggerConfiguration()
    .MinimumLevel.ControlledBy(SerilogHelper.LoggingLevelSwitch)
    .WriteTo.Console(outputTemplate: SerilogHelper.OutputTemplate)
    .CreateLogger();

try {
    Startup.InitializeJsonSerializer(); // This is a workaround for the fact that some static constructors are not called
    RootCommand rootCommand = new("CompatCircuit Multi-Party Computation Program") {
        GeneratePresharedCommand(),
        GenerateExampleConfigCommand(),
        RunSingleExecutorCommand(),
        RunMpcExecutorCommand(),
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