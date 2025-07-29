using Anonymous.CollaborativeZkVm.ZkPrograms;
using Anonymous.CollaborativeZkVm.ZkPrograms.Examples;
using Anonymous.CollaborativeZkVmExperiment.ExperimentConfigs;
using Anonymous.CollaborativeZkVmExperiment.ExperimentFourZkPrograms;
using Anonymous.CollaborativeZkVmExperiment.ExperimentOneExecutors;
using Anonymous.CollaborativeZkVmExperiment.ExperimentRandomPublicInputGenerators;
using Anonymous.CollaborativeZkVmExperiment.ExperimentTwoThreeZkPrograms;
using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.CompatCircuits.R1csCircuits;
using Anonymous.CompatCircuitCore.Computation;
using Anonymous.CompatCircuitCore.Computation.MultiParty;
using Anonymous.CompatCircuitCore.Computation.MultiParty.Network;
using Anonymous.CompatCircuitCore.Computation.MultiParty.SharedStorages;
using Anonymous.CompatCircuitCore.Computation.SingleParty;
using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.DaBitPrioPlus;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.EdaBitsKai;
using Anonymous.CompatCircuitCore.RandomGenerators;
using Anonymous.CompatCircuitCore.SerilogHelpers;
using Serilog;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Net;
using Startup = Anonymous.CollaborativeZkVmExperiment.Startup;

// _ = Trace.Listeners.Add(new ConsoleTraceListener(true));
_ = Trace.Listeners.Add(new SerilogTraceListener.SerilogTraceListener());

string instanceName = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "default";
string logFilenamePrefix = $"log.{instanceName}";

// Note: don't modify these filenames. Otherwise, please also modify the bash commands involved in ExperimentOneDistributeFilesCommand

const string ExperimentConfigFileName = "ExpConfig.json";
ExperimentConfig GetExperimentConfig() {
    string filename = ExperimentConfigFileName;
    using Stream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
    return JsonSerializerHelper.Deserialize<ExperimentConfig>(stream, JsonConfig.JsonSerializerOptions) ?? throw new Exception($"Invalid experiment configuration file {filename}");
}

string GetMpcConfigFileName(int partyIndex) => $"MpcConfig.{partyIndex}.json";
MpcConfig GetMpcConfig(int partyIndex) {
    string filename = GetMpcConfigFileName(partyIndex);
    using Stream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
    return JsonSerializerHelper.Deserialize<MpcConfig>(stream, JsonConfig.JsonSerializerOptions) ?? throw new Exception($"Invalid MpcConfig file {filename}");
}

string GetFieldBeaverFileName(int i) => $"FieldBeaver.{i}.bin";
ICountingEnumerator<FieldBeaverTripleShare> GetFieldBeaverEnumerator(int partyIndex) => new FieldBeaverTripleShareFileEnumerator(File.Open(GetFieldBeaverFileName(partyIndex), FileMode.Open, FileAccess.Read, FileShare.Read), ArithConfig.FieldFactory);

string GetBoolBeaverFileName(int i) => $"BoolBeaver.{i}.bin";
ICountingEnumerator<BoolBeaverTripleShare> GetBoolBeaverEumerator(int partyIndex) => new BoolBeaverTripleShareFileEnumerator(File.Open(GetBoolBeaverFileName(partyIndex), FileMode.Open, FileAccess.Read, FileShare.Read));

string GetEdaBitsFileName(int i) => $"edaBits.{i}.bin";
ICountingEnumerator<EdaBitsKaiShare> GetEdaBitsEnumerator(int partyIndex) => new EdaBitsKaiShareFileEnumerator(File.Open(GetEdaBitsFileName(partyIndex), FileMode.Open, FileAccess.Read, FileShare.Read), ArithConfig.FieldFactory);

string GetDaBitPrioPlusFileName(int i) => $"daBitPrioPlus.{i}.bin";
ICountingEnumerator<DaBitPrioPlusShare> GetDaBitPrioPlusEnumerator(int partyIndex) => new DaBitPrioPlusShareFileEnumerator(File.Open(GetDaBitPrioPlusFileName(partyIndex), FileMode.Open, FileAccess.Read, FileShare.Read), ArithConfig.FieldFactory);

const string PublicInputFileName = $"PublicInput.bin";
ICountingEnumerator<Field> GetPublicInputEnumerator() => new ExperimentRandomPublicInputFileEnumerator(File.Open(PublicInputFileName, FileMode.Open, FileAccess.Read, FileShare.Read), ArithConfig.FieldFactory);

string GetProgramInstanceFileName(string name, int i) => $"{name}.instance.{i}.json";

Command GenerateExperimentConfigCommand() {
    Option<int> partyCountOption = new("--parties", "The number of parties in secure multi-party computation") { IsRequired = true };

    Command command = new("gen-config-example", "Generate experiment configuration file example") { partyCountOption };

    void Handle(InvocationContext invocationContext) {
        int partyCount = invocationContext.ParseResult.GetValueForOption(partyCountOption);
        if (partyCount > 100) {
            throw new Exception("Party count is too large. Please manually write the configuration file.");
        }

        ExperimentConfig config = new() { PartyIPAddresses = Enumerable.Range(0, partyCount).Select(i => $"127.0.0.{100 + i}").Select(IPAddress.Parse).ToList() };
        string expConfigFileName = ExperimentConfigFileName;
        using (Stream stream = File.Open(expConfigFileName, FileMode.Create, FileAccess.Write)) {
            JsonSerializerHelper.Serialize(stream, config, JsonConfig.JsonSerializerOptions);
        }
        Serilog.Log.Information($"Example configuration file {expConfigFileName} generated");
    }

    command.SetHandler(Handle);
    return command;
}

Command ExperimentOnePrepareFilesCommand() {
    Option<bool> repeatPresharedOption = new(name: "--unsafe-repeat-preshared", description: "Repeatly use preshared values. This is extremely unsafe and only meant for debugging or evaluation purpose.", getDefaultValue: () => false) { IsRequired = false };

    Command command = new("exp-1-prepare-files", "Generate preshared files including configuration files for Experiment 1") { repeatPresharedOption };

    void Handle(InvocationContext invocationContext) {
        bool repeatPreshared = invocationContext.ParseResult.GetValueForOption(repeatPresharedOption)!;

        ExperimentConfig expConfig = GetExperimentConfig();
        int partyCount = expConfig.PartyIPAddresses.Count;

        void GenerateConfig() {
            for (int partyIndex = 0; partyIndex < partyCount; partyIndex++) {
                MpcConfig mpcConfig = new() {
                    PartyIPAddresses = expConfig.PartyIPAddresses,
                    MyID = partyIndex,
                    MyIPAddress = expConfig.PartyIPAddresses[partyIndex],
                    TickMS = 0,
                    TimeoutMS = 3000,
                };

                string filename = GetMpcConfigFileName(partyIndex);
                using Stream stream = File.Open(filename, FileMode.Create, FileAccess.Write);
                JsonSerializerHelper.Serialize(stream, mpcConfig, JsonConfig.JsonSerializerOptions);
            }
        }

        GenerateConfig();

        // Note: these numbers are manually specified to match the need of ExperimentOneCircuits.*CircuitBoardGenerator.RepeatCount
        // Also, increasing partyCount requires more preshared values because of Bit-Decomposition. So the number here is bigger than the need.
        int publicInputCount = 10;
        int arithTripleCount = 2000000;
        int boolTripleCount = 1000000;
        int edaBitsPairCount = 1000;
        int daBitPrioPlusCount = 100000;

        if (repeatPreshared) {
            publicInputCount = 10;
            arithTripleCount = 10;
            boolTripleCount = 10;
            edaBitsPairCount = 10;
            daBitPrioPlusCount = 10;
        }

        void GenerateInputs() {
            // Generate public inputs
            {
                Serilog.Log.Information($"Generate {publicInputCount} random public inputs");
                string filename = PublicInputFileName;
                using Stream stream = File.Open(filename, FileMode.Create, FileAccess.Write);
                new ExperimentRandomPublicInputGenerator() { FieldFactory = ArithConfig.FieldFactory }.GenerateExperimentRandomPublicInputFile(stream, publicInputCount);
            }
        }
        GenerateInputs();

        void GeneratePreshared() {
            // Generate FieldBeaverTripleShares
            {
                Serilog.Log.Information($"Generate {arithTripleCount} FieldBeaverTripleShares");
                Func<int, string> GetFileName = GetFieldBeaverFileName;
                List<Stream> streams = Enumerable.Range(0, partyCount).Select(GetFileName).Select(filename => File.Open(filename, FileMode.Create, FileAccess.Write) as Stream).ToList();
                FieldBeaverTripleGenerator fieldBeaverTripleGenerator = new() { FieldFactory = ArithConfig.FieldFactory, FieldSecretSharing = ArithConfig.FieldSecretSharing };
                fieldBeaverTripleGenerator.GenerateBeaverTripleShareFileForAllParties(streams, partyCount, arithTripleCount, leaveOpen: false);
            }

            // Generate BoolBeaverTripleShareLists
            {
                Serilog.Log.Information($"Generate {boolTripleCount} BoolBeaverTripleShares");
                Func<int, string> GetFileName = GetBoolBeaverFileName;
                List<Stream> streams = Enumerable.Range(0, partyCount).Select(GetFileName).Select(filename => File.Open(filename, FileMode.Create, FileAccess.Write) as Stream).ToList();
                BoolBeaverTripleGenerator boolBeaverTripleGenerator = new() { RandomGenerator = RandomConfig.RandomGenerator, BoolSecretSharing = ArithConfig.BoolSecretSharing };
                boolBeaverTripleGenerator.GenerateBeaverTripleShareFileForAllParties(streams, partyCount, boolTripleCount, leaveOpen: false);
            }

            // Generate edaBitsKaiShares
            {
                Serilog.Log.Information($"Generate {edaBitsPairCount} edaBitsKaiShares");
                Func<int, string> GetFileName = GetEdaBitsFileName;
                List<Stream> streams = Enumerable.Range(0, partyCount).Select(GetFileName).Select(filename => File.Open(filename, FileMode.Create, FileAccess.Write) as Stream).ToList();
                EdaBitsKaiGenerator edaBitsKaiGenerator = new(ArithConfig.BitSize, partyCount, ArithConfig.FieldFactory, ArithConfig.FieldSecretSharing, ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator);
                edaBitsKaiGenerator.GenerateEdaBitsShareFileForAllParties(streams, edaBitsPairCount, leaveOpen: false);
            }

            // Generate daBitPrioPlusShares
            {
                Serilog.Log.Information($"Generate {daBitPrioPlusCount} daBitPrioPlusShares");
                Func<int, string> GetFileName = GetDaBitPrioPlusFileName;
                List<Stream> streams = Enumerable.Range(0, partyCount).Select(GetFileName).Select(filename => File.Open(filename, FileMode.Create, FileAccess.Write) as Stream).ToList();
                DaBitPrioPlusGenerator daBitPrioPlusGenerator = new() { FieldFactory = ArithConfig.FieldFactory, FieldSecretSharing = ArithConfig.FieldSecretSharing, RandomGenerator = RandomConfig.RandomGenerator, BoolSecretSharing = ArithConfig.BoolSecretSharing };
                daBitPrioPlusGenerator.GenerateDaBitPrioPlusShareFileForAllParties(streams, partyCount, daBitPrioPlusCount, leaveOpen: false);
            }
        }
        GeneratePreshared();
    }

    command.SetHandler(Handle);
    return command;
}

Command ExperimentOneDistributeFilesCommand() {
    Command command = new("exp-1-distribute-files", "Distribute preshared files including configuration files for Experiment 1");

    void Handle() {
        ExperimentConfig expConfig = GetExperimentConfig();
        List<string> commands = [
            $"parties=({string.Join(" ", expConfig.PartyIPAddresses)})",
            $"username=root",
            "for ipaddr in \"${parties[@]}\"; do",
            "\tscp -o \"StrictHostKeyChecking no\" -o \"UserKnownHostsFile /dev/null\" *.bin MpcConfig.*.json $username@$ipaddr:~/exp1;",
            "done",
        ];
        foreach (string command in commands) {
            Console.WriteLine(command);
        }
        Console.Error.WriteLine("Please manually execute the Bash commands above");
    }

    command.SetHandler(Handle);
    return command;
}

Command ExperimentOneRunSinglePartyCommand() {
    Option<bool> repeatPresharedOption = new(name: "--unsafe-repeat-preshared", description: "Repeatly use preshared values. This is extremely unsafe and only meant for debugging or evaluation purpose.", getDefaultValue: () => false) { IsRequired = false };

    Command command = new("exp-1-run-single", "Run Experiment 1 as single party") { repeatPresharedOption };

    async Task Handle(InvocationContext invocationContext) {
        bool repeatPreshared = invocationContext.ParseResult.GetValueForOption(repeatPresharedOption)!;

        Serilog.Log.Information("Preparing...");

        ICountingEnumerator<Field> publicInputEnumerator = GetPublicInputEnumerator();
        if (repeatPreshared) {
            publicInputEnumerator = new CountingEnumerator<Field>(new RepeatingEnumerator<Field>(publicInputEnumerator));
        }

        IMpcExecutorFactory mpcExecutorFactory = new SingleExecutorFactory();

        ExperimentOneExecutor experimentExecutor = new() {
            MyID = 0,
            MpcExecutorFactory = mpcExecutorFactory,
            IsSingleParty = true,
            RandomPublicValueEnumerator = publicInputEnumerator,
        };

        Serilog.Log.Information("Executing...");
        try {
            ExperimentOneExecuteResult result = await experimentExecutor.Execute();

            // Print time cost
            Serilog.Log.Information($"Total time cost: {result.TotalTime.TotalSeconds:F6} seconds");
            Serilog.Log.Information("Step time costs:");
            foreach ((string stepName, TimeSpan timeSpan) in result.StepTimes) {
                Serilog.Log.Information($"{stepName}: {timeSpan.TotalSeconds:F6} seconds");
            }

            // Print how many inputs are used
            Serilog.Log.Information($"Random public inputs used: {publicInputEnumerator.Count}");

            foreach ((string name, R1csCircuitWithValues? r1cs) in result.R1csCircuitsWithValues) {
                using Stream stream = File.Open(Path.Combine($"{instanceName}.{name}.single.r1cs.json"), FileMode.Create, FileAccess.Write);
                JsonSerializerHelper.Serialize(stream, r1cs, JsonConfig.JsonSerializerOptions);
            }

            Serilog.Log.Information("Self-Verifying R1CS constraints");
            foreach ((string name, R1csCircuitWithValues? r1cs) in result.R1csCircuitsWithValues) {
                r1cs.SelfVerify();
            }
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

Command ExperimentOneRunMultiPartyInThreadCommand() {
    Option<bool> repeatPresharedOption = new(name: "--unsafe-repeat-preshared", description: "Repeatly use preshared values. This is extremely unsafe and only meant for debugging or evaluation purpose.", getDefaultValue: () => false) { IsRequired = false };

    Command command = new("exp-1-run-mpc-thread", "Run Experiment 1 in MPC as multi parties running in threads in a single node") { repeatPresharedOption };

    void Handle(InvocationContext invocationContext) {
        bool repeatPreshared = invocationContext.ParseResult.GetValueForOption(repeatPresharedOption)!;

        ExperimentConfig expConfig = GetExperimentConfig();
        int partyCount = expConfig.PartyIPAddresses.Count;

        Serilog.Log.Information("Preparing...");
        List<ICountingEnumerator<FieldBeaverTripleShare>> fieldBeaverTripleShareEnumeratorAllParties = Enumerable.Range(0, partyCount).Select(GetFieldBeaverEnumerator).ToList();
        List<ICountingEnumerator<BoolBeaverTripleShare>> boolBeaverTripleShareEnumeratorAllParties = Enumerable.Range(0, partyCount).Select(GetBoolBeaverEumerator).ToList();
        List<ICountingEnumerator<EdaBitsKaiShare>> edaBitsShareEnumeratorAllParties = Enumerable.Range(0, partyCount).Select(GetEdaBitsEnumerator).ToList();
        List<ICountingEnumerator<DaBitPrioPlusShare>> daBitPrioPlusShareEnumeratorAllParties = Enumerable.Range(0, partyCount).Select(GetDaBitPrioPlusEnumerator).ToList();
        List<ICountingEnumerator<Field>> publicInputEnumeratorAllParties = Enumerable.Range(0, partyCount).Select(_ => GetPublicInputEnumerator()).ToList();

        if (repeatPreshared) {
            fieldBeaverTripleShareEnumeratorAllParties = fieldBeaverTripleShareEnumeratorAllParties.Select(enumerator => new CountingEnumerator<FieldBeaverTripleShare>(new RepeatingEnumerator<FieldBeaverTripleShare>(enumerator)) as ICountingEnumerator<FieldBeaverTripleShare>).ToList();
            boolBeaverTripleShareEnumeratorAllParties = boolBeaverTripleShareEnumeratorAllParties.Select(enumerator => new CountingEnumerator<BoolBeaverTripleShare>(new RepeatingEnumerator<BoolBeaverTripleShare>(enumerator)) as ICountingEnumerator<BoolBeaverTripleShare>).ToList();
            edaBitsShareEnumeratorAllParties = edaBitsShareEnumeratorAllParties.Select(enumerator => new CountingEnumerator<EdaBitsKaiShare>(new RepeatingEnumerator<EdaBitsKaiShare>(enumerator)) as ICountingEnumerator<EdaBitsKaiShare>).ToList();
            daBitPrioPlusShareEnumeratorAllParties = daBitPrioPlusShareEnumeratorAllParties.Select(enumerator => new CountingEnumerator<DaBitPrioPlusShare>(new RepeatingEnumerator<DaBitPrioPlusShare>(enumerator)) as ICountingEnumerator<DaBitPrioPlusShare>).ToList();
            publicInputEnumeratorAllParties = publicInputEnumeratorAllParties.Select(enumerator => new CountingEnumerator<Field>(new RepeatingEnumerator<Field>(enumerator)) as ICountingEnumerator<Field>).ToList();
        }

        DummyCountingMpcClient mpcClient = new(partyCount);
        IMpcSharedStorageSessionManager mpcSharedStorageSessionManager = new DummyMpcSharedStorageSessionManager(mpcClient);
        IMpcSharedStorageFactory mpcSharedStorageFactory = new MpcSharedStorageCachedFactory() { ExpireAfterRetrievalCount = partyCount };

        List<ExperimentOneExecutor> expExecutors = [];
        for (int myID = 0; myID < partyCount; myID++) {
            MpcExecutorConfig mpcExecutorConfig = new() {
                MyID = myID,
                PartyCount = partyCount,
                TickMS = 0,
            };
            IMpcExecutorFactory mpcExecutorFactory = new MpcExecutorFactory(
                mpcExecutorConfig,
                mpcSharedStorageSessionManager,
                mpcSharedStorageFactory,
                fieldBeaverTripleShareEnumeratorAllParties[myID],
                boolBeaverTripleShareEnumeratorAllParties[myID],
                edaBitsShareEnumeratorAllParties[myID],
                daBitPrioPlusShareEnumeratorAllParties[myID]);

            ExperimentOneExecutor expExecutor = new() {
                MyID = myID,
                MpcExecutorFactory = mpcExecutorFactory,
                IsSingleParty = false,
                RandomPublicValueEnumerator = publicInputEnumeratorAllParties[myID],
            };
            expExecutors.Add(expExecutor);
        }

        Task<ExperimentOneExecuteResult>[] zkProgramExecutorTasks = Enumerable.Range(0, partyCount).Select(myID => Task.Run(expExecutors[myID].Execute)).ToArray();
        Task.WaitAll(zkProgramExecutorTasks);
        List<ExperimentOneExecuteResult> results = zkProgramExecutorTasks.Select(task => task.Result).ToList();

        for (int j = 0; j < partyCount; j++) {
            ExperimentOneExecuteResult result = results[j];
            Serilog.Log.Information($"==== MPC Party {j} ====");

            foreach ((string name, R1csCircuitWithValues? r1cs) in result.R1csCircuitsWithValues) {
                using Stream stream = File.Open($"{instanceName}.{name}.party{j}.r1cs.json", FileMode.Create, FileAccess.Write);
                JsonSerializerHelper.Serialize(stream, r1cs, JsonConfig.JsonSerializerOptions);
            }

            // Print time cost
            Serilog.Log.Information($"Total time cost: {result.TotalTime.TotalSeconds:F6} seconds");
            Serilog.Log.Information("Step time costs:");
            foreach ((string stepName, TimeSpan timeSpan) in result.StepTimes) {
                Serilog.Log.Information($"{stepName}: {timeSpan.TotalSeconds:F6} seconds");
            }

            // Print how many shares are used
            Serilog.Log.Information($"Random public inputs used: {publicInputEnumeratorAllParties[j].Count}");
            Serilog.Log.Information($"FieldBeaverTripleShare used: {fieldBeaverTripleShareEnumeratorAllParties[j].Count}");
            Serilog.Log.Information($"BoolBeaverTripleShare used: {boolBeaverTripleShareEnumeratorAllParties[j].Count}");
            Serilog.Log.Information($"EdaBitsKaiShare used: {edaBitsShareEnumeratorAllParties[j].Count}");
            Serilog.Log.Information($"DaBitPrioPlusShare used: {daBitPrioPlusShareEnumeratorAllParties[j].Count}");
        }

        // Print network traffic usage
        Serilog.Log.Information($"Total sent (all parties): {mpcClient.TotalBytesSent} bytes");
    }

    command.SetHandler(Handle);
    return command;
}

Command ExperimentOneRunMultiPartyCommand() {
    Option<int> partyIndexOption = new(name: "--party", description: "Party index") { IsRequired = true };
    Option<bool> repeatPresharedOption = new(name: "--unsafe-repeat-preshared", description: "Repeatly use preshared values. This is extremely unsafe and only meant for debugging or evaluation purpose.", getDefaultValue: () => false) { IsRequired = false };

    Command command = new("exp-1-run-mpc", "Run Experiment 1 in MPC") { partyIndexOption, repeatPresharedOption };

    async Task Handle(InvocationContext invocationContext) {
        int partyIndex = invocationContext.ParseResult.GetValueForOption(partyIndexOption);
        bool repeatPreshared = invocationContext.ParseResult.GetValueForOption(repeatPresharedOption)!;

        MpcConfig mpcConfig = GetMpcConfig(partyIndex);
        if (mpcConfig.MyID != partyIndex) {
            throw new Exception("Party index mismatch in MPC configuration");
        }

        Serilog.Log.Information("Preparing...");
        ICountingEnumerator<FieldBeaverTripleShare> fieldBeaverTripleShareEnumerator = GetFieldBeaverEnumerator(partyIndex);
        ICountingEnumerator<BoolBeaverTripleShare> boolBeaverTripleShareEnumerator = GetBoolBeaverEumerator(partyIndex);
        ICountingEnumerator<EdaBitsKaiShare> edaBitsKaiShareEnumerator = GetEdaBitsEnumerator(partyIndex);
        ICountingEnumerator<DaBitPrioPlusShare> daBitPrioPlusShareEnumerator = GetDaBitPrioPlusEnumerator(partyIndex);
        ICountingEnumerator<Field> publicInputEnumerator = GetPublicInputEnumerator();

        if (repeatPreshared) {
            fieldBeaverTripleShareEnumerator = new CountingEnumerator<FieldBeaverTripleShare>(new RepeatingEnumerator<FieldBeaverTripleShare>(fieldBeaverTripleShareEnumerator));
            boolBeaverTripleShareEnumerator = new CountingEnumerator<BoolBeaverTripleShare>(new RepeatingEnumerator<BoolBeaverTripleShare>(boolBeaverTripleShareEnumerator));
            edaBitsKaiShareEnumerator = new CountingEnumerator<EdaBitsKaiShare>(new RepeatingEnumerator<EdaBitsKaiShare>(edaBitsKaiShareEnumerator));
            daBitPrioPlusShareEnumerator = new CountingEnumerator<DaBitPrioPlusShare>(new RepeatingEnumerator<DaBitPrioPlusShare>(daBitPrioPlusShareEnumerator));
            publicInputEnumerator = new CountingEnumerator<Field>(new RepeatingEnumerator<Field>(publicInputEnumerator));
        }

        Serilog.Log.Information("Connecting to MPC network...");

        UdpMpcClient mpcClient = new(new NetMpcClientConfig(mpcConfig));
        IMpcExecutorFactory mpcExecutorFactory = new MpcExecutorFactory(
            new MpcExecutorConfig(mpcConfig), new MpcSharedStorageSessionManager(mpcClient), new MpcSharedStorageFactory(),
            fieldBeaverTripleShareEnumerator, boolBeaverTripleShareEnumerator, edaBitsKaiShareEnumerator, daBitPrioPlusShareEnumerator);

        ExperimentOneExecutor experimentExecutor = new() {
            MyID = mpcConfig.MyID,
            MpcExecutorFactory = mpcExecutorFactory,
            IsSingleParty = false,
            RandomPublicValueEnumerator = publicInputEnumerator,
        };

        try {
            mpcClient.Start();
            ExperimentOneExecuteResult result = await experimentExecutor.Execute();

            foreach ((string name, R1csCircuitWithValues? r1cs) in result.R1csCircuitsWithValues) {
                using Stream stream = File.Open($"{instanceName}.{name}.party{partyIndex}.r1cs.json", FileMode.Create, FileAccess.Write);
                JsonSerializerHelper.Serialize(stream, r1cs, JsonConfig.JsonSerializerOptions);
            }

            // Print time cost
            Serilog.Log.Information($"Total time cost: {result.TotalTime.TotalSeconds:F6} seconds");
            Serilog.Log.Information("Step time costs:");
            foreach ((string stepName, TimeSpan timeSpan) in result.StepTimes) {
                Serilog.Log.Information($"{stepName}: {timeSpan.TotalSeconds:F6} seconds");
            }

            // Print how many shares are used
            Serilog.Log.Information($"Random public inputs used: {publicInputEnumerator.Count}");
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

Command ExperimentTwoGenerateZkProgramInstanceCommand() {
    Command command = new("exp-2-gen-zk-program-instance", "Generate zero-knowledge program instances for Experiment 2");

    void Handle() {
        ExperimentConfig expConfig = GetExperimentConfig();
        int partyCount = expConfig.PartyIPAddresses.Count;

        List<IZkProgramExampleGenerator> programGenerators = [
            new ExperimentTwoZkProgram1Generator(),
            new ExperimentTwoZkProgram2Generator(),
            new ExperimentTwoZkProgram3Generator(),
            new ExperimentTwoZkProgram4Generator(),
            new ExperimentTwoZkProgram5Generator(),
        ];
        Dictionary<string, ZkProgramExample> examples = programGenerators.Select(generator => generator.GetZkProgram()).Select(program => (program.Name, program)).ToDictionary(); ;

        foreach ((_, ZkProgramExample zkProgramExample) in examples) {
            List<ZkProgramInstance> programInstances = zkProgramExample.GetZkProgramInstances(partyCount);

            for (int partyIndex = 0; partyIndex < partyCount; partyIndex++) {
                ZkProgramInstance programInstance = programInstances[partyIndex];
                using Stream stream = File.Open($"{zkProgramExample.CodeName}.instance.{partyIndex}.json", FileMode.Create, FileAccess.Write);
                JsonSerializerHelper.Serialize(stream, programInstance, JsonConfig.JsonSerializerOptions);
            }
        }
    }

    command.SetHandler(Handle);
    return command;
}

Command ExperimentFourGenerateZkProgramInstanceCommand() {
    Command command = new("exp-4-gen-zk-program-instance", "Generate zero-knowledge program instances for Experiment 4");

    void Handle() {
        ExperimentConfig expConfig = GetExperimentConfig();
        int partyCount = expConfig.PartyIPAddresses.Count;

        List<IZkProgramExampleGenerator> programGenerators = [
            new ExperimentFourZkProgramBubbleSortGenerator(),
            new ExperimentFourZkProgramFibonacciGenerator(),
            new ExperimentFourZkProgramIncreasingSubsequenceGenerator(),
            new ExperimentFourZkProgramRangeQueryGenerator(),
            new ExperimentFourZkProgramSlidingWindowGenerator(),
            new ExperimentFourZkProgramBinarySearchGenerator(),
            new ExperimentFourZkProgramSetIntersecionGenerator()
        ];
        Dictionary<string, ZkProgramExample> examples = programGenerators.Select(generator => generator.GetZkProgram()).Select(program => (program.Name, program)).ToDictionary(); ;

        foreach ((_, ZkProgramExample zkProgramExample) in examples) {
            List<ZkProgramInstance> programInstances = zkProgramExample.GetZkProgramInstances(partyCount);

            for (int partyIndex = 0; partyIndex < partyCount; partyIndex++) {
                ZkProgramInstance programInstance = programInstances[partyIndex];
                using Stream stream = File.Open($"{zkProgramExample.CodeName}.instance.{partyIndex}.json", FileMode.Create, FileAccess.Write);
                JsonSerializerHelper.Serialize(stream, programInstance, JsonConfig.JsonSerializerOptions);
            }
        }
    }
    command.SetHandler(Handle);
    return command;
}

Command ExperimentThreeGenerateZkProgramInstanceCommand() {
    Command command = new("exp-3-gen-zk-program-instance", "Generate zero-knowledge program instances for Experiment 3");

    void Handle() {
        ExperimentConfig expConfig = GetExperimentConfig();
        int partyCount = expConfig.PartyIPAddresses.Count;

        List<int> programStepCounts = [4, 16, 20, 32, 50, 64];

        List<IZkProgramExampleGenerator> programGenerators = programStepCounts.Select(count => new ExperimentThreeZkProgramGenerator($"exp3_{count}") { ProgramStepCount = count } as IZkProgramExampleGenerator).ToList();

        Dictionary<string, ZkProgramExample> examples = programGenerators.Select(generator => generator.GetZkProgram()).Select(program => (program.Name, program)).ToDictionary(); ;

        foreach ((_, ZkProgramExample zkProgramExample) in examples) {
            List<ZkProgramInstance> programInstances = zkProgramExample.GetZkProgramInstances(partyCount);

            for (int partyIndex = 0; partyIndex < partyCount; partyIndex++) {
                ZkProgramInstance programInstance = programInstances[partyIndex];
                using Stream stream = File.Open($"{zkProgramExample.CodeName}.instance.{partyIndex}.json", FileMode.Create, FileAccess.Write);
                JsonSerializerHelper.Serialize(stream, programInstance, JsonConfig.JsonSerializerOptions);
            }
        }
    }

    command.SetHandler(Handle);
    return command;
}

Command RunMpcZkVmCommand() {
    // This is almost the same with the one in CollaborativeZkVm except for parameters
    Option<int> partyIndexOption = new(name: "--party", description: "Party index") { IsRequired = true };
    Option<FileInfo> programInstanceFileOption = new(name: "--program-instance", description: "The ZK program instance file") { IsRequired = true };
    Option<bool> repeatPresharedOption = new(name: "--unsafe-repeat-preshared", description: "Repeatly use preshared values. This is extremely unsafe and only meant for debugging or evaluation purpose.", getDefaultValue: () => false) { IsRequired = false };

    Command command = new("run-mpc-zkvm", "Run the MPC zkVM") { programInstanceFileOption, repeatPresharedOption, partyIndexOption };

    async Task Handle(InvocationContext invocationContext) {
        FileInfo programInstanceFile = invocationContext.ParseResult.GetValueForOption(programInstanceFileOption)!;
        bool repeatPreshared = invocationContext.ParseResult.GetValueForOption(repeatPresharedOption)!;
        int partyIndex = invocationContext.ParseResult.GetValueForOption(partyIndexOption);

        MpcConfig mpcConfig = GetMpcConfig(partyIndex);
        if (mpcConfig.MyID != partyIndex) {
            throw new Exception("Party index mismatch in MPC configuration");
        }

        Serilog.Log.Information("Preparing...");
        ICountingEnumerator<FieldBeaverTripleShare> fieldBeaverTripleShareEnumerator = GetFieldBeaverEnumerator(partyIndex);
        ICountingEnumerator<BoolBeaverTripleShare> boolBeaverTripleShareEnumerator = GetBoolBeaverEumerator(partyIndex);
        ICountingEnumerator<EdaBitsKaiShare> edaBitsKaiShareEnumerator = GetEdaBitsEnumerator(partyIndex);
        ICountingEnumerator<DaBitPrioPlusShare> daBitPrioPlusShareEnumerator = GetDaBitPrioPlusEnumerator(partyIndex);
        ICountingEnumerator<Field> publicInputEnumerator = GetPublicInputEnumerator();

        if (repeatPreshared) {
            fieldBeaverTripleShareEnumerator = new CountingEnumerator<FieldBeaverTripleShare>(new RepeatingEnumerator<FieldBeaverTripleShare>(fieldBeaverTripleShareEnumerator));
            boolBeaverTripleShareEnumerator = new CountingEnumerator<BoolBeaverTripleShare>(new RepeatingEnumerator<BoolBeaverTripleShare>(boolBeaverTripleShareEnumerator));
            edaBitsKaiShareEnumerator = new CountingEnumerator<EdaBitsKaiShare>(new RepeatingEnumerator<EdaBitsKaiShare>(edaBitsKaiShareEnumerator));
            daBitPrioPlusShareEnumerator = new CountingEnumerator<DaBitPrioPlusShare>(new RepeatingEnumerator<DaBitPrioPlusShare>(daBitPrioPlusShareEnumerator));
            publicInputEnumerator = new CountingEnumerator<Field>(new RepeatingEnumerator<Field>(publicInputEnumerator));
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
            OnR1csCircuitWithValuesGeneratedAsync = (string name, R1csCircuitWithValues r1cs) => {
                using Stream stream = File.Open($"{instanceName}.{name}.party{mpcConfig.MyID}.r1cs.json", FileMode.Create, FileAccess.Write);
                JsonSerializerHelper.Serialize(stream, r1cs, JsonConfig.JsonSerializerOptions);
            },
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

Command RunMpcZkVmInThreadCommand() {
    Option<bool> repeatPresharedOption = new(name: "--unsafe-repeat-preshared", description: "Repeatly use preshared values. This is extremely unsafe and only meant for debugging or evaluation purpose.", getDefaultValue: () => false) { IsRequired = false };
    Option<string> programInstanceNameOption = new(name: "--program-instance-name", description: "The name prefix of ZK program instance files") { IsRequired = true };

    Command command = new("run-mpc-zkvm-thread", "Run the MPC zkVM as multi parties running in threads in a single node") { repeatPresharedOption, programInstanceNameOption };

    void Handle(InvocationContext invocationContext) {
        bool repeatPreshared = invocationContext.ParseResult.GetValueForOption(repeatPresharedOption)!;
        string programInstanceName = invocationContext.ParseResult.GetValueForOption(programInstanceNameOption)!;

        ExperimentConfig expConfig = GetExperimentConfig();
        int partyCount = expConfig.PartyIPAddresses.Count;

        Serilog.Log.Information("Preparing...");
        List<ICountingEnumerator<FieldBeaverTripleShare>> fieldBeaverTripleShareEnumeratorAllParties = Enumerable.Range(0, partyCount).Select(GetFieldBeaverEnumerator).ToList();
        List<ICountingEnumerator<BoolBeaverTripleShare>> boolBeaverTripleShareEnumeratorAllParties = Enumerable.Range(0, partyCount).Select(GetBoolBeaverEumerator).ToList();
        List<ICountingEnumerator<EdaBitsKaiShare>> edaBitsShareEnumeratorAllParties = Enumerable.Range(0, partyCount).Select(GetEdaBitsEnumerator).ToList();
        List<ICountingEnumerator<DaBitPrioPlusShare>> daBitPrioPlusShareEnumeratorAllParties = Enumerable.Range(0, partyCount).Select(GetDaBitPrioPlusEnumerator).ToList();

        if (repeatPreshared) {
            fieldBeaverTripleShareEnumeratorAllParties = fieldBeaverTripleShareEnumeratorAllParties.Select(enumerator => new CountingEnumerator<FieldBeaverTripleShare>(new RepeatingEnumerator<FieldBeaverTripleShare>(enumerator)) as ICountingEnumerator<FieldBeaverTripleShare>).ToList();
            boolBeaverTripleShareEnumeratorAllParties = boolBeaverTripleShareEnumeratorAllParties.Select(enumerator => new CountingEnumerator<BoolBeaverTripleShare>(new RepeatingEnumerator<BoolBeaverTripleShare>(enumerator)) as ICountingEnumerator<BoolBeaverTripleShare>).ToList();
            edaBitsShareEnumeratorAllParties = edaBitsShareEnumeratorAllParties.Select(enumerator => new CountingEnumerator<EdaBitsKaiShare>(new RepeatingEnumerator<EdaBitsKaiShare>(enumerator)) as ICountingEnumerator<EdaBitsKaiShare>).ToList();
            daBitPrioPlusShareEnumeratorAllParties = daBitPrioPlusShareEnumeratorAllParties.Select(enumerator => new CountingEnumerator<DaBitPrioPlusShare>(new RepeatingEnumerator<DaBitPrioPlusShare>(enumerator)) as ICountingEnumerator<DaBitPrioPlusShare>).ToList();
        }

        DummyCountingMpcClient mpcClient = new(partyCount);
        IMpcSharedStorageSessionManager mpcSharedStorageSessionManager = new DummyMpcSharedStorageSessionManager(mpcClient);
        IMpcSharedStorageFactory mpcSharedStorageFactory = new MpcSharedStorageCachedFactory() { ExpireAfterRetrievalCount = partyCount };

        List<ZkProgramExecutor> zkProgramExecutors = [];
        for (int myID = 0; myID < partyCount; myID++) {
            MpcExecutorConfig mpcExecutorConfig = new() {
                MyID = myID,
                PartyCount = partyCount,
                TickMS = 0,
            };
            IMpcExecutorFactory mpcExecutorFactory = new MpcExecutorFactory(
                mpcExecutorConfig,
                mpcSharedStorageSessionManager,
                mpcSharedStorageFactory,
                fieldBeaverTripleShareEnumeratorAllParties[myID],
                boolBeaverTripleShareEnumeratorAllParties[myID],
                edaBitsShareEnumeratorAllParties[myID],
                daBitPrioPlusShareEnumeratorAllParties[myID]);

            ZkProgramInstance programInstance;
            {
                programInstance = JsonSerializerHelper.Deserialize<ZkProgramInstance>(
                    File.Open(GetProgramInstanceFileName(programInstanceName, myID), FileMode.Open, FileAccess.Read, FileShare.Read), JsonConfig.JsonSerializerOptions) ?? throw new Exception("Cannot parse ZK program instance file");
                if (programInstance.PartyCount != partyCount) {
                    throw new Exception("Party count mismatch");
                }

                if (programInstance.MyID != myID) {
                    throw new Exception("Party ID mismatch");
                }
            }

            int myIDCaptured = myID;  // Capture the variable for the lambda

            ZkProgramExecutor zkProgramExecutor = new() {
                MyID = myID,
                MpcExecutorFactory = mpcExecutorFactory,
                IsSingleParty = false,
                ZkProgramInstance = programInstance,
                OnR1csCircuitWithValuesGeneratedAsync = (string name, R1csCircuitWithValues r1cs) => {
                    using Stream stream = File.Open($"{instanceName}.{name}.party{myIDCaptured}.r1cs.json", FileMode.Create, FileAccess.Write);
                    JsonSerializerHelper.Serialize(stream, r1cs, JsonConfig.JsonSerializerOptions);
                },
            };
            zkProgramExecutors.Add(zkProgramExecutor);
        }

        Task<ZkProgramExecuteResult>[] zkProgramExecutorTasks = Enumerable.Range(0, partyCount).Select(myID => Task.Run(zkProgramExecutors[myID].Execute)).ToArray();
        Task.WaitAll(zkProgramExecutorTasks);
        List<ZkProgramExecuteResult> results = zkProgramExecutorTasks.Select(task => task.Result).ToList();

        for (int j = 0; j < partyCount; j++) {
            ZkProgramExecuteResult result = results[j];
            Serilog.Log.Information($"==== MPC Party {j} ====");

            Serilog.Log.Information($"PublicOutputs: [{string.Join(", ", result.PublicOutputs)}]");
            Serilog.Log.Information($"GlobalStepCounter: {result.GlobalStepCounter}");

            // Print time cost
            Serilog.Log.Information($"Total time cost: {result.TotalTime.TotalSeconds:F6} seconds");
            Serilog.Log.Information("Step time costs:");
            foreach ((string stepName, TimeSpan timeSpan) in result.StepTimes) {
                Serilog.Log.Information($"{stepName}: {timeSpan.TotalSeconds:F6} seconds");
            }

            // Print how many shares are used
            Serilog.Log.Information($"FieldBeaverTripleShare used: {fieldBeaverTripleShareEnumeratorAllParties[j].Count}");
            Serilog.Log.Information($"BoolBeaverTripleShare used: {boolBeaverTripleShareEnumeratorAllParties[j].Count}");
            Serilog.Log.Information($"EdaBitsKaiShare used: {edaBitsShareEnumeratorAllParties[j].Count}");
            Serilog.Log.Information($"DaBitPrioPlusShare used: {daBitPrioPlusShareEnumeratorAllParties[j].Count}");
        }

        // Print network traffic usage
        Serilog.Log.Information($"Total sent (all parties): {mpcClient.TotalBytesSent} bytes");
    }

    command.SetHandler(Handle);
    return command;
}

Command GeneratePresharedCommand() {
    // Similar like the one in CompatCircuitCore, but it writes to /dev/null and is able to use an unsafe random generator.
    Option<int> partyCountOption = new("--parties", "The number of parties in secure multi-party computation") { IsRequired = true };
    Option<int> arithTripleCountOption = new("--field-beaver-triples", "The number of field beaver triples to be used. For example, 100000. Each beaver triple can only be used once, the larger, the better") { IsRequired = true };
    Option<int> boolTripleCountOption = new("--bool-beaver-triples", "The number of boolean beaver triples to be used. For example, 10000000. Each beaver triple can only be used once, the larger, the better") { IsRequired = true };
    Option<int> edaBitsPairCountOption = new("--edaBits-pair", "The number of edaBits pair to be used. For example, 1000. Each edaBits pair can only be used once, the larger, the better") { IsRequired = true };
    Option<int> daBitPrioPlusCountOption = new("--daBitPrioPlus-pair", "The number of daBitPrioPlus to be used. For example, 100000. Each daBitPrioPlus can only be used once, the larger, the better") { IsRequired = true };
    Option<bool> unsafeUseFakeRandomSourceOption = new("--unsafe-use-fake-random-source", description: "Use a fake random source to replace the cryptographic random generator. This is extremely unsafe and only meant for debugging or evaluation purpose.", getDefaultValue: () => false) { };

    Command command = new("gen-preshared", "Generate (and discard) preshared beaver triples, edaBits pairs, and daBitPrioPlus pairs.") { partyCountOption, arithTripleCountOption, boolTripleCountOption, edaBitsPairCountOption, daBitPrioPlusCountOption, unsafeUseFakeRandomSourceOption };

    command.SetHandler((int partyCount, int arithTripleCount, int boolTripleCount, int edaBitsPairCount, int daBitPrioPlusCount, bool unsafeUseFakeRandomSource) => {
        if (unsafeUseFakeRandomSource) {
            RandomConfig.RandomGenerator.Value = new UnsafeDummyRandomGenerator();
        }

        DateTimeOffset totalStartTime = DateTimeOffset.Now;

        // Generate FieldBeaverTripleShares
        {
            DateTimeOffset startTime = DateTimeOffset.Now;
            Serilog.Log.Information($"Generate {arithTripleCount} FieldBeaverTripleShares");
            List<Stream> streams = Enumerable.Repeat(Stream.Null, partyCount).ToList();
            FieldBeaverTripleGenerator fieldBeaverTripleGenerator = new() { FieldFactory = ArithConfig.FieldFactory, FieldSecretSharing = ArithConfig.FieldSecretSharing };
            fieldBeaverTripleGenerator.GenerateBeaverTripleShareFileForAllParties(streams, partyCount, arithTripleCount, leaveOpen: false);
            DateTimeOffset endTime = DateTimeOffset.Now;
            TimeSpan timecost = endTime - startTime;
            Serilog.Log.Information($"FieldBeaverTripleShare time cost: {timecost.TotalSeconds:F6} seconds");
        }

        // Generate BoolBeaverTripleShareLists
        {
            DateTimeOffset startTime = DateTimeOffset.Now;
            Serilog.Log.Information($"Generate {boolTripleCount} BoolBeaverTripleShares");
            List<Stream> streams = Enumerable.Repeat(Stream.Null, partyCount).ToList();
            BoolBeaverTripleGenerator boolBeaverTripleGenerator = new() { RandomGenerator = RandomConfig.RandomGenerator, BoolSecretSharing = ArithConfig.BoolSecretSharing };
            boolBeaverTripleGenerator.GenerateBeaverTripleShareFileForAllParties(streams, partyCount, boolTripleCount, leaveOpen: false);
            DateTimeOffset endTime = DateTimeOffset.Now;
            TimeSpan timecost = endTime - startTime;
            Serilog.Log.Information($"BoolBeaverTripleShare time cost: {timecost.TotalSeconds:F6} seconds");
        }

        // Generate edaBitsKaiShares
        {
            DateTimeOffset startTime = DateTimeOffset.Now;
            Serilog.Log.Information($"Generate {edaBitsPairCount} edaBitsKaiShares");
            List<Stream> streams = Enumerable.Repeat(Stream.Null, partyCount).ToList();
            EdaBitsKaiGenerator edaBitsKaiGenerator = new(ArithConfig.BitSize, partyCount, ArithConfig.FieldFactory, ArithConfig.FieldSecretSharing, ArithConfig.BoolSecretSharing, RandomConfig.RandomGenerator);
            edaBitsKaiGenerator.GenerateEdaBitsShareFileForAllParties(streams, edaBitsPairCount, leaveOpen: false);
            DateTimeOffset endTime = DateTimeOffset.Now;
            TimeSpan timecost = endTime - startTime;
            Serilog.Log.Information($"EdaBitsKaiShare time cost: {timecost.TotalSeconds:F6} seconds");
        }

        // Generate daBitPrioPlusShares
        {
            DateTimeOffset startTime = DateTimeOffset.Now;
            Serilog.Log.Information($"Generate {daBitPrioPlusCount} daBitPrioPlusShares");
            List<Stream> streams = Enumerable.Repeat(Stream.Null, partyCount).ToList();
            DaBitPrioPlusGenerator daBitPrioPlusGenerator = new() { FieldFactory = ArithConfig.FieldFactory, FieldSecretSharing = ArithConfig.FieldSecretSharing, RandomGenerator = RandomConfig.RandomGenerator, BoolSecretSharing = ArithConfig.BoolSecretSharing };
            daBitPrioPlusGenerator.GenerateDaBitPrioPlusShareFileForAllParties(streams, partyCount, daBitPrioPlusCount, leaveOpen: false);
            DateTimeOffset endTime = DateTimeOffset.Now;
            TimeSpan timecost = endTime - startTime;
            Serilog.Log.Information($"DaBitPrioPlusShare time cost: {timecost.TotalSeconds:F6} seconds");
        }

        DateTimeOffset totalEndTime = DateTimeOffset.Now;
        TimeSpan totalTimecost = totalEndTime - totalStartTime;

        Serilog.Log.Information($"Total time cost: {totalTimecost.TotalSeconds:F6} seconds");
    }, partyCountOption, arithTripleCountOption, boolTripleCountOption, edaBitsPairCountOption, daBitPrioPlusCountOption, unsafeUseFakeRandomSourceOption);

    return command;
}

Serilog.Log.Logger = new Serilog.LoggerConfiguration()
    .MinimumLevel.ControlledBy(SerilogHelper.LoggingLevelSwitch)
    .WriteTo.Console(outputTemplate: SerilogHelper.OutputTemplate)
    .WriteTo.File($"{logFilenamePrefix}.{DateTimeOffset.Now:yyyy-MM-dd.HH.mm.ss}.txt", outputTemplate: SerilogHelper.OutputTemplate) // TODO: make this configurable
    .CreateLogger();

try {
    Startup.InitializeJsonSerializer();
    RootCommand rootCommand = new("CompatCircuit zkVM Experiment Program") {
        GenerateExperimentConfigCommand(),
        ExperimentOnePrepareFilesCommand(),
        ExperimentOneDistributeFilesCommand(),
        ExperimentOneRunSinglePartyCommand(),
        ExperimentOneRunMultiPartyInThreadCommand(),
        ExperimentOneRunMultiPartyCommand(),
        ExperimentTwoGenerateZkProgramInstanceCommand(),
        ExperimentThreeGenerateZkProgramInstanceCommand(),
        ExperimentFourGenerateZkProgramInstanceCommand(),
        RunMpcZkVmCommand(),
        RunMpcZkVmInThreadCommand(),
        GeneratePresharedCommand(),
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