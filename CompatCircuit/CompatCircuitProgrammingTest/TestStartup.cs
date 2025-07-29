using Anonymous.CompatCircuitCore.SerilogHelpers;
using Serilog;
using Startup = Anonymous.CompatCircuitProgramming.Startup;

namespace Anonymous.CompatCircuitProgrammingTest;

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
