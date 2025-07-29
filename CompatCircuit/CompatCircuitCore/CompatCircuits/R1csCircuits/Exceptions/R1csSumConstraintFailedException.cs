namespace Anonymous.CompatCircuitCore.CompatCircuits.R1csCircuits.Exceptions;
public class R1csSumConstraintFailedException : R1csConstraintFailedExceptionBase {
    public R1csSumConstraintFailedException(string message, int leftWire, int rightWire, int resultWire) : base(message, leftWire, rightWire, resultWire) { }
}
