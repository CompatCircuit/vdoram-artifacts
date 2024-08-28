namespace SadPencil.CompatCircuitCore.Computation.SingleParty;
public class SingleExecutorFactory : IMpcExecutorFactory {
    public int NextSessionID { get; private set; } = 0;
    public SingleExecutor NextExecutor() => new() { LoggerPrefix = $"Single{this.NextSessionID++}" };
    IMpcExecutor IMpcExecutorFactory.NextExecutor() => this.NextExecutor();
}
