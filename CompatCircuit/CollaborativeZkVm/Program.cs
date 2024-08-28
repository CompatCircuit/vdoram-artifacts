using SadPencil.CollaborativeZkVm.ZkPrograms;
using SadPencil.CollaborativeZkVm.ZkPrograms.Examples;
using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.CompatCircuits.R1csCircuits;
using SadPencil.CompatCircuitCore.Computation;
using SadPencil.CompatCircuitCore.Computation.MultiParty;
using SadPencil.CompatCircuitCore.Computation.MultiParty.Network;
using SadPencil.CompatCircuitCore.Computation.MultiParty.SharedStorages;
using SadPencil.CompatCircuitCore.Computation.SingleParty;
using SadPencil.CompatCircuitCore.Extensions;
using SadPencil.CompatCircuitCore.GlobalConfig;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.DaBitPrioPlus;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.EdaBitsKai;
using SadPencil.CompatCircuitCore.SerilogHelpers;
using Serilog;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using Startup = SadPencil.CollaborativeZkVm.Startup;

// _ = Trace.Listeners.Add(new ConsoleTraceListener(true));
_ = Trace.Listeners.Add(new SerilogTraceListener.SerilogTraceListener());

string instanceName = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "default";
string logFilenamePrefix = $"log.{instanceName}";

Command GenerateExampleZkProgramCommand() {
    Option<DirectoryInfo> outputFolderOption = new(name: "--output-folder", description: "Where to save the example zk program files") { IsRequired = true };

    Command command = new("generate-example-zk-programs", "Generate example zk programs") { outputFolderOption };

    void Handle(InvocationContext invocationContext) {
        DirectoryInfo outputFolder = invocationContext.ParseResult.GetValueForOption(outputFolderOption)!;

        foreach ((_, ZkProgramExample zkProgramExample) in ZkProgramExamples.Examples) {
            using Stream stream = File.Open(Path.Combine(outputFolder.FullName, $"{zkProgramExample.CodeName}.example.json"), FileMode.Create, FileAccess.Write);
            JsonSerializerHelper.Serialize(stream, zkProgramExample, JsonConfig.JsonSerializerOptions);
        }
    }

    command.SetHandler(Handle);
    return command;
}

Command GenerateZkProgramInstanceExampleCommand() {
    Option<DirectoryInfo> outputFolderOption = new(name: "--output-folder", description: "Where to save the example zk program instance files") { IsRequired = true };
    Option<int> partyCountOption = new(name: "--party-count", description: "Number of parties", getDefaultValue: () => 2) { IsRequired = false };

    Command command = new("generate-zk-program-instance-example", "Generate an example zk program instance") { outputFolderOption, partyCountOption };

    void Handle(InvocationContext invocationContext) {
        DirectoryInfo outputFolder = invocationContext.ParseResult.GetValueForOption(outputFolderOption)!;
        int partyCount = invocationContext.ParseResult.GetValueForOption(partyCountOption);

        foreach ((_, ZkProgramExample zkProgramExample) in ZkProgramExamples.Examples) {
            List<ZkProgramInstance> programInstances = zkProgramExample.GetZkProgramInstances(partyCount);
            for (int partyIndex = 0; partyIndex < partyCount; partyIndex++) {
                ZkProgramInstance programInstance = programInstances[partyIndex];
                using Stream stream = File.Open(Path.Combine(outputFolder.FullName, $"{zkProgramExample.CodeName}.instance.{partyIndex}.json"), FileMode.Create, FileAccess.Write);
                JsonSerializerHelper.Serialize(stream, programInstance, JsonConfig.JsonSerializerOptions);
            }
        }
    }

    command.SetHandler(Handle);
    return command;
}

Command RunSingleZkVmCommand() {
    Option<FileInfo> programInstanceFileOption = new(name: "--program-instance", description: "The ZK program instance file") { IsRequired = true };
    Option<DirectoryInfo> outputFolderOption = new(name: "--output-folder", description: "Where to save the output files") { IsRequired = true };

    Command command = new("run-single-zkvm", "Run zkVM in single party") { programInstanceFileOption, outputFolderOption };

    async Task Handle(InvocationContext invocationContext) {
        FileInfo programInstanceFile = invocationContext.ParseResult.GetValueForOption(programInstanceFileOption)!;
        DirectoryInfo outputFolder = invocationContext.ParseResult.GetValueForOption(outputFolderOption)!;

        Serilog.Log.Information("Preparing...");
        ZkProgramInstance programInstance;
        {
            programInstance = JsonSerializerHelper.Deserialize<ZkProgramInstance>(
                File.Open(programInstanceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read), JsonConfig.JsonSerializerOptions) ?? throw new Exception("Cannot parse ZK program instance file");
            if (programInstance.PartyCount != 1) {
                throw new Exception("Party count mismatch");
            }

            if (programInstance.MyID != 0) {
                throw new Exception("Party ID mismatch");
            }
        }

        IMpcExecutorFactory mpcExecutorFactory = new SingleExecutorFactory();

        ZkProgramExecutor zkProgramExecutor = new() {
            ZkProgramInstance = programInstance,
            MyID = 0,
            MpcExecutorFactory = mpcExecutorFactory,
            IsSingleParty = true,
            OnR1csCircuitWithValuesGenerated = new Progress<(string, R1csCircuitWithValues)>(arg => {
                (string name, R1csCircuitWithValues r1cs) = arg;
                using Stream stream = File.Open(Path.Combine(outputFolder.FullName, $"{instanceName}.{name}.single.r1cs.json"), FileMode.Create, FileAccess.Write);
                JsonSerializerHelper.Serialize(stream, r1cs, JsonConfig.JsonSerializerOptions);

                r1cs.SelfVerify();
            }),
        };

        try {
            ZkProgramExecuteResult result = await zkProgramExecutor.Execute();

            // Print time cost
            Serilog.Log.Information($"Total time cost: {result.TotalTime.TotalSeconds:F6} seconds");
            Serilog.Log.Information("Step time costs:");
            foreach ((string stepName, TimeSpan timeSpan) in result.StepTimes) {
                Serilog.Log.Information($"{stepName}: {timeSpan.TotalSeconds:F6} seconds");
            }

            Serilog.Log.Information($"PublicOutputs: {string.Join(", ", result.PublicOutputs)}");
            Serilog.Log.Information($"GlobalStepCounter: {result.GlobalStepCounter}");

        }
        catch (Exception ex) {
            Serilog.Log.Error(ex.Message, ex);
            Debugger.Break();
            throw;
        }
    }

    command.SetHandler(Handle);
    return command;

}

Command RunMpcZkVmCommand() {
    Option<FileInfo> mpcConfigFileOption = new(name: "--config", description: "The MPC configuration file") { IsRequired = true };
    Option<FileInfo> fieldBeaverFileOption = new(name: "--field-beaver", description: "File containing this party's secret shares of field beaver triples") { IsRequired = true };
    Option<FileInfo> boolBeaverFileOption = new(name: "--bool-beaver", description: "File containing this party's secret shares of boolean beaver triples") { IsRequired = true };
    Option<FileInfo> edaBitsFileOption = new(name: "--edaBits", description: "File containing this party's secret shares of edaBits pairs") { IsRequired = true };
    Option<FileInfo> daBitPrioPlusFileOption = new(name: "--daBitPrioPlus", description: "File containing this party's secret shares of daBitPrioPlus") { IsRequired = true };
    Option<FileInfo> programInstanceFileOption = new(name: "--program-instance", description: "The ZK program instance file") { IsRequired = true };
    Option<DirectoryInfo> outputFolderOption = new(name: "--output-folder", description: "Where to save the output files") { IsRequired = true };
    Option<bool> repeatPresharedOption = new(name: "--unsafe-repeat-preshared", description: "Repeatly use preshared values. This is extremely unsafe and only meant for debugging or evaluation purpose.", getDefaultValue: () => false) { IsRequired = false };

    Command command = new("run-mpc-zkvm", "Run the MPC zkVM") {
        mpcConfigFileOption, fieldBeaverFileOption, boolBeaverFileOption, edaBitsFileOption, daBitPrioPlusFileOption, programInstanceFileOption, outputFolderOption, repeatPresharedOption };

    async Task Handle(InvocationContext invocationContext) {
        FileInfo mpcConfigFile = invocationContext.ParseResult.GetValueForOption(mpcConfigFileOption)!;
        FileInfo fieldBeaverFile = invocationContext.ParseResult.GetValueForOption(fieldBeaverFileOption)!;
        FileInfo boolBeaverFile = invocationContext.ParseResult.GetValueForOption(boolBeaverFileOption)!;
        FileInfo edaBitsFile = invocationContext.ParseResult.GetValueForOption(edaBitsFileOption)!;
        FileInfo daBitPrioPlusFile = invocationContext.ParseResult.GetValueForOption(daBitPrioPlusFileOption)!;
        FileInfo programInstanceFile = invocationContext.ParseResult.GetValueForOption(programInstanceFileOption)!;
        DirectoryInfo outputFolder = invocationContext.ParseResult.GetValueForOption(outputFolderOption)!;
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

        ZkProgramInstance programInstance;
        {
            programInstance = JsonSerializerHelper.Deserialize<ZkProgramInstance>(
                File.Open(programInstanceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read), JsonConfig.JsonSerializerOptions) ?? throw new Exception("Cannot parse ZK program instance file");
            if (programInstance.PartyCount != mpcConfig.PartyCount) {
                throw new Exception("Party count mismatch");
            }

            if (programInstance.MyID != mpcConfig.MyID) {
                throw new Exception("Party ID mismatch");
            }
        }

        Serilog.Log.Information("Connecting to MPC network...");

        UdpMpcClient mpcClient = new(new NetMpcClientConfig(mpcConfig));
        IMpcExecutorFactory mpcExecutorFactory = new MpcExecutorFactory(
            new MpcExecutorConfig(mpcConfig), new MpcSharedStorageSessionManager(mpcClient), new MpcSharedStorageFactory(),
            fieldBeaverTripleShareEnumerator, boolBeaverTripleShareEnumerator, edaBitsKaiShareEnumerator, daBitPrioPlusShareEnumerator);

        ZkProgramExecutor zkProgramExecutor = new() {
            ZkProgramInstance = programInstance,
            MyID = mpcConfig.MyID,
            MpcExecutorFactory = mpcExecutorFactory,
            IsSingleParty = false,
            OnR1csCircuitWithValuesGenerated = new Progress<(string, R1csCircuitWithValues)>(arg => {
                (string name, R1csCircuitWithValues r1cs) = arg;
                using Stream stream = File.Open(Path.Combine(outputFolder.FullName, $"{instanceName}.{name}.party{mpcConfig.MyID}.r1cs.json"), FileMode.Create, FileAccess.Write);
                JsonSerializerHelper.Serialize(stream, r1cs, JsonConfig.JsonSerializerOptions);
            }),
        };

        try {
            mpcClient.Start();
            ZkProgramExecuteResult result = await zkProgramExecutor.Execute();

            Serilog.Log.Information($"PublicOutputs: [{string.Join(", ", result.PublicOutputs)}]");
            Serilog.Log.Information($"GlobalStepCounter: {result.GlobalStepCounter}");

            // Print time cost
            Serilog.Log.Information($"Total time cost: {result.TotalTime.TotalSeconds:F6} seconds");
            Serilog.Log.Information("Step time costs:");
            foreach ((string stepName, TimeSpan timeSpan) in result.StepTimes) {
                Serilog.Log.Information($"{stepName}: {timeSpan.TotalSeconds:F6} seconds");
            }

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
    }

    command.SetHandler(Handle);
    return command;
}

Command RunFakeR1csReshareCommand() {
    Option<int> sourcePartyCountOption = new(name: "--source-parties", description: "The number of parties in secure multi-party computation") { IsRequired = true };
    Option<int> targetPartyCountOption = new(name: "--target-parties", description: "The number of parties in secure multi-party computation") { IsRequired = true };
    Option<DirectoryInfo> inputFolderOption = new(name: "--input-folder", description: "Where to load the input r1cs files") { IsRequired = true };
    Option<DirectoryInfo> outputFolderOption = new(name: "--output-folder", description: "Where to save the r1cs files") { IsRequired = true };
    Command command = new("run-fake-r1cs-reshare", "Reconstructing secrets from R1CS json files. Only for debugging purpose.") { sourcePartyCountOption, targetPartyCountOption, inputFolderOption, outputFolderOption };

    void Handle(InvocationContext invocationContext) {
        int sourcePartyCount = invocationContext.ParseResult.GetValueForOption(sourcePartyCountOption);
        int targetPartyCount = invocationContext.ParseResult.GetValueForOption(targetPartyCountOption);
        DirectoryInfo inputFolder = invocationContext.ParseResult.GetValueForOption(inputFolderOption)!;
        DirectoryInfo outputFolder = invocationContext.ParseResult.GetValueForOption(outputFolderOption)!;

        if (sourcePartyCount < 2) {
            throw new Exception("Source party count should be at least 2.");
        }

        if (targetPartyCount < 2) {
            throw new Exception("Target party count should be at least 2.");
        }

        // Find all files ends with .party0.r1cs.json
        FileInfo[] r1csFiles = inputFolder.GetFiles("*.party0.r1cs.json", SearchOption.TopDirectoryOnly);
        if (r1csFiles.Length == 0) {
            throw new Exception("No R1CS files found");
        }

        foreach (FileInfo r1csFile in r1csFiles) {
            // Get file name, excluding the .party0.r1cs.json part
            string r1csFileNamePrefix = r1csFile.Name[..^".party0.r1cs.json".Length];

            Serilog.Log.Information($"Processing {r1csFileNamePrefix}...");

            // Find party1.r1cs.json, party2.r1cs.json, etc.
            List<FileInfo> partyR1csFiles = [r1csFile];
            bool allPartyR1csFilesExist = true;
            for (int partyIndex = 1; partyIndex < sourcePartyCount; partyIndex++) {
                FileInfo partyR1csFile = new(Path.Combine(inputFolder.FullName, $"{r1csFileNamePrefix}.party{partyIndex}.r1cs.json"));
                if (!partyR1csFile.Exists) {
                    Serilog.Log.Warning($"Warning: Cannot find {partyR1csFile.FullName}");
                    allPartyR1csFilesExist = false;
                    break;
                }

                partyR1csFiles.Add(partyR1csFile);
            }

            if (!allPartyR1csFilesExist) {
                Serilog.Log.Warning($"Skip {r1csFile.FullName}");
                continue;
            }

            // Load all R1CS files
            List<R1csCircuitWithValues> r1csJsons = partyR1csFiles.Select(file => JsonSerializerHelper.Deserialize<R1csCircuitWithValues>(
                File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read), JsonConfig.JsonSerializerOptions) ?? throw new Exception("Cannot parse R1CS file")).ToList();

            // Merge secret shares
            int valueCount = r1csJsons[0].WireValues.Count;
            if (r1csJsons.Any(x => x.WireValues.Count != valueCount)) {
                throw new Exception("Value count mismatch");
            }

            List<MpcValue> exposedValues = [];
            for (int i = 0; i < valueCount; i++) {
                List<MpcValue> values = r1csJsons.Select(x => x.WireValues[i]).ToList();
                bool isSecretShare = values[0].IsSecretShare;
                if (values.Any(x => x.IsSecretShare != isSecretShare)) {
                    throw new Exception("Secret share attribute mismatch");
                }

                if (isSecretShare) {
                    List<Field> shares = values.Select(x => x.Value).ToList();
                    Field value = ArithConfig.FieldSecretSharing.RecoverFromShares(sourcePartyCount, shares);
                    exposedValues.Add(new MpcValue { IsSecretShare = true, Value = value }); // keep secret share attribute
                }
                else {
                    Field value = values[0].Value;
                    if (values.Any(x => x.Value != value)) {
                        throw new Exception("Value mismatch");
                    }

                    exposedValues.Add(new MpcValue { IsSecretShare = false, Value = value });
                }
            }

            // Reshare
            List<List<MpcValue>> newShares = Enumerable.Range(0, targetPartyCount).Select(_ => new List<MpcValue>()).ToList();

            for (int i = 0; i < valueCount; i++) {
                Trace.Assert(newShares.All(list => list.Count == i));

                if (exposedValues[i].IsSecretShare) {
                    List<Field> shares = ArithConfig.FieldSecretSharing.MakeShares(targetPartyCount, exposedValues[i].Value);
                    Trace.Assert(shares.Count == targetPartyCount);

                    for (int j = 0; j < targetPartyCount; j++) {
                        newShares[j].Add(new MpcValue() { IsSecretShare = true, Value = shares[j] });
                    }
                }
                else {
                    for (int j = 0; j < targetPartyCount; j++) {
                        newShares[j].Add(exposedValues[i]);
                    }
                }
            }

            // Write R1CS
            for (int j = 0; j < targetPartyCount; j++) {
                R1csCircuitWithValues r1cs = new() {
                    ProductConstraints = r1csJsons[0].ProductConstraints,
                    PublicWireCount = r1csJsons[0].PublicWireCount,
                    SumConstraints = r1csJsons[0].SumConstraints,
                    WireCount = r1csJsons[0].WireCount,
                    WireValues = newShares[j],
                };

                using Stream stream = File.Open(Path.Combine(outputFolder.FullName, $"{r1csFileNamePrefix}.party{j}.r1cs.json"), FileMode.Create, FileAccess.Write);
                JsonSerializerHelper.Serialize(stream, r1cs, JsonConfig.JsonSerializerOptions);
            }

            GC.Collect();
        }
    }
    command.SetHandler(Handle);
    return command;
}

Command RunFakeVerifyCommand() {
    Option<int> partyCountOption = new(name: "--parties", description: "The number of parties in secure multi-party computation") { IsRequired = true };
    Option<DirectoryInfo> inputFolderOption = new(name: "--input-folder", description: "Where to load the input r1cs files") { IsRequired = true };

    Command command = new("run-fake-verify", "Verifying R1CS constraints by reconstructing secrets. Only for debugging purpose.") { partyCountOption, inputFolderOption };

    void Handle(InvocationContext invocationContext) {
        int partyCount = invocationContext.ParseResult.GetValueForOption(partyCountOption);
        DirectoryInfo inputFolder = invocationContext.ParseResult.GetValueForOption(inputFolderOption)!;

        // Find all files ends with .party0.r1cs.json
        FileInfo[] r1csFiles = inputFolder.GetFiles("*.party0.r1cs.json", SearchOption.TopDirectoryOnly);
        if (r1csFiles.Length == 0) {
            throw new Exception("No R1CS files found");
        }

        foreach (FileInfo r1csFile in r1csFiles) {
            // Get file name, excluding the .party0.r1cs.json part
            string r1csFileNamePrefix = r1csFile.Name[..^".party0.r1cs.json".Length];

            // Find party1.r1cs.json, party2.r1cs.json, etc.
            List<FileInfo> partyR1csFiles = [r1csFile];
            bool allPartyR1csFilesExist = true;
            for (int partyIndex = 1; partyIndex < partyCount; partyIndex++) {
                FileInfo partyR1csFile = new(Path.Combine(inputFolder.FullName, $"{r1csFileNamePrefix}.party{partyIndex}.r1cs.json"));
                if (!partyR1csFile.Exists) {
                    Serilog.Log.Warning($"Warning: Cannot find {partyR1csFile.FullName}");
                    allPartyR1csFilesExist = false;
                    break;
                }

                partyR1csFiles.Add(partyR1csFile);
            }

            if (!allPartyR1csFilesExist) {
                Serilog.Log.Warning($"Skip {r1csFile.FullName}");
                continue;
            }

            // Load all R1CS files
            List<R1csCircuitWithValues> r1csJsons = partyR1csFiles.Select(file => JsonSerializerHelper.Deserialize<R1csCircuitWithValues>(
                File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read), JsonConfig.JsonSerializerOptions) ?? throw new Exception("Cannot parse R1CS file")).ToList();

            // Merge secret shares
            int valueCount = r1csJsons[0].WireValues.Count;
            if (r1csJsons.Any(x => x.WireValues.Count != valueCount)) {
                throw new Exception("Value count mismatch");
            }

            List<MpcValue> exposedValues = [];
            for (int i = 0; i < valueCount; i++) {
                List<MpcValue> values = r1csJsons.Select(x => x.WireValues[i]).ToList();
                bool isSecretShare = values[0].IsSecretShare;
                if (values.Any(x => x.IsSecretShare != isSecretShare)) {
                    throw new Exception("Secret share attribute mismatch");
                }

                if (isSecretShare) {
                    List<Field> shares = values.Select(x => x.Value).ToList();
                    Field value = ArithConfig.FieldSecretSharing.RecoverFromShares(partyCount, shares);
                    exposedValues.Add(new MpcValue { IsSecretShare = false, Value = value }); // remove secret share attribute
                }
                else {
                    Field value = values[0].Value;
                    if (values.Any(x => x.Value != value)) {
                        throw new Exception("Value mismatch");
                    }

                    exposedValues.Add(new MpcValue { IsSecretShare = false, Value = value });
                }
            }

            // Verify R1CS constraints
            R1csCircuitWithValues r1CsCircuitWithValues = new() {
                ProductConstraints = r1csJsons[0].ProductConstraints,
                SumConstraints = r1csJsons[0].SumConstraints,
                WireCount = r1csJsons[0].WireCount,
                PublicWireCount = r1csJsons[0].PublicWireCount,
                WireValues = exposedValues,
            };

            try {
                Serilog.Log.Information($"Verifying {r1csFile.FullName}...");
                r1CsCircuitWithValues.SelfVerify();
                Serilog.Log.Information($"Verification passed for {r1csFile.FullName}");
            }
            catch (Exception ex) {
                Serilog.Log.Warning($"Verification failed for {r1csFile.FullName}: {ex.Message}", ex);
            }

            GC.Collect();
        }
    }

    command.SetHandler(Handle);
    return command;
}

Serilog.Log.Logger = new Serilog.LoggerConfiguration()
    .MinimumLevel.ControlledBy(SerilogHelper.LoggingLevelSwitch)
    .WriteTo.Console(outputTemplate: SerilogHelper.OutputTemplate)
    .WriteTo.File($"{logFilenamePrefix}.{DateTimeOffset.Now:yyyy-MM-dd.HH.mm.ss}.txt", outputTemplate: SerilogHelper.OutputTemplate) // TODO: make this configurable
    .CreateLogger();

try {
    Startup.InitializeJsonSerializer();
    RootCommand rootCommand = new("CompatCircuit zkVM Program") {
        GenerateExampleZkProgramCommand(),
        GenerateZkProgramInstanceExampleCommand(),
        RunSingleZkVmCommand(),
        RunMpcZkVmCommand(),
        RunFakeVerifyCommand(),
        RunFakeR1csReshareCommand(),
    };

    return await rootCommand.InvokeAsync(args);
}
catch (Exception ex) {
    Serilog.Log.Error(ex, ex.Message);
    Debugger.Break();
    return 1;
}
finally {
    await Serilog.Log.CloseAndFlushAsync();
}