using Serilog.Core;

namespace Anonymous.CompatCircuitCore.SerilogHelpers;
public static class SerilogHelper {
    public static Serilog.Core.LoggingLevelSwitch LoggingLevelSwitch { get; } = new LoggingLevelSwitch() { MinimumLevel = Serilog.Events.LogEventLevel.Information };
    public static string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}";
}
