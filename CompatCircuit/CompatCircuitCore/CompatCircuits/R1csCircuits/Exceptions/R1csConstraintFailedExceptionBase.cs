namespace Anonymous.CompatCircuitCore.CompatCircuits.R1csCircuits.Exceptions;
public abstract class R1csConstraintFailedExceptionBase : Exception {
    public int LeftWire { get; }
    public int RightWire { get; }
    public int ResultWire { get; }
    public R1csConstraintFailedExceptionBase(string message, int leftWire, int rightWire, int resultWire) : base(message) {
        this.LeftWire = leftWire;
        this.RightWire = rightWire;
        this.ResultWire = resultWire;
    }
}
