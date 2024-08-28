﻿using SadPencil.CompatCircuitCore.SerilogHelpers;
using Serilog;
using Startup = SadPencil.CollaborativeZkVm.Startup;

namespace SadPencil.CollaborativeZkVmTest;

[TestClass]
public static class TestStartup {
    [AssemblyInitialize]
    public static void Initialize(TestContext context) {
        InitializeSerilog();
        Startup.InitializeJsonSerializer();
    }

    private static void InitializeSerilog() => Serilog.Log.Logger = new Serilog.LoggerConfiguration()
        .MinimumLevel.ControlledBy(SerilogHelper.LoggingLevelSwitch)
        .WriteTo.Trace(outputTemplate: SerilogHelper.OutputTemplate)
        .CreateLogger();
}
