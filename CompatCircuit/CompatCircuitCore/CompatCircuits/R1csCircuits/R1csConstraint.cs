using System.Diagnostics.CodeAnalysis;

namespace Anonymous.CompatCircuitCore.CompatCircuits.R1csCircuits;
public record R1csConstraint {
    public required int LeftWire { get; init; }
    public required int RightWire { get; init; }
    public required int ResultWire { get; init; }

    public R1csConstraint() { }

    [SetsRequiredMembers]
    public R1csConstraint(int leftWire, int rightWire, int resultWire) {
        this.LeftWire = leftWire;
        this.RightWire = rightWire;
        this.ResultWire = resultWire;
    }

    public sealed override string ToString() => $"({this.LeftWire}|{this.RightWire}|{this.ResultWire})";
    public static R1csConstraint FromString(string str) {
        string[] parts = str.Trim('(', ')').Split("|");
        return parts.Length != 3
            ? throw new FormatException("Invalid R1csConstraint string.")
            : new R1csConstraint(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
    }

    public void Deconstruct(out int leftWire, out int rightWire, out int resultWire) {
        leftWire = this.LeftWire;
        rightWire = this.RightWire;
        resultWire = this.ResultWire;
    }

    static R1csConstraint() => R1csConstraintJsonConverter.Initialize();
}
