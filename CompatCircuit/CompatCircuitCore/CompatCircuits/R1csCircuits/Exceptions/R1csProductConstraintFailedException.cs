namespace Anonymous.CompatCircuitCore.CompatCircuits.R1csCircuits.Exceptions;
public class R1csProductConstraintFailedException : R1csConstraintFailedExceptionBase {
    public R1csProductConstraintFailedException(string message, int leftWire, int rightWire, int resultWire) : base(message, leftWire, rightWire, resultWire) { }
}
