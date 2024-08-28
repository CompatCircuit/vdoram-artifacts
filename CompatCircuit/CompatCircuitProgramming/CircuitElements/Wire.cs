using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.Extensions;
using SadPencil.CompatCircuitCore.GlobalConfig;
using System.Numerics;

namespace SadPencil.CompatCircuitProgramming.CircuitElements;
public class Wire : object {
    public required WireType WireType { get; set; }

    private string? _name = null;
    public required string? Name {
        get => this._name;
        set {
            if (this._name is not null && this.WireType != WireType.OperationResult) {
                throw new InvalidOperationException("Input wire name cannot be changed");
            }
            this._name = value;
        }
    }
    public required Field? ConstValue { get; set; }
    public bool IsPublicOutput { get; set; } = false;
    public bool IsPrivateOutput { get; set; } = false;
    public int Layer { get; set; } = 0;
    private Wire() { }

    // Note: Wire explicitly extends Object.
    public sealed override bool Equals(object? obj) => base.Equals(obj);
    public sealed override int GetHashCode() => base.GetHashCode();

    public Wire Clone() => new() { ConstValue = this.ConstValue, WireType = this.WireType, IsPublicOutput = this.IsPublicOutput, Name = this.Name };
    public override string ToString() {
        string nameStr = string.IsNullOrEmpty(this.Name) ? string.Empty : $"Name: {this.Name.ToLiteral()}";
        string typeStr = $"Type: {this.WireType}";
        string constStr = this.ConstValue is null ? string.Empty : $"Value: {this.ConstValue}";
        string publicOutputStr = this.IsPublicOutput ? "PublicOutput" : string.Empty;
        string privateOutputStr = this.IsPrivateOutput ? "PrivateOutput" : string.Empty;
        List<string> outputStrings = [nameStr, typeStr, constStr, publicOutputStr, privateOutputStr];

        return string.Join(", ", outputStrings.Where(str => !string.IsNullOrEmpty(str)));
    }

    public static Wire NewPublicInputWire(string? name) => new() { ConstValue = null, Name = name, WireType = WireType.PublicInput };
    public static Wire NewPrivateInputWire(string? name) => new() { ConstValue = null, Name = name, WireType = WireType.PrivateInput };
    public static Wire NewConstantWire(Field constValue, string? name) => new() { ConstValue = constValue, Name = name, WireType = WireType.Constant };
    public static Wire NewConstantWire(Field constValue) => new() { ConstValue = constValue, Name = $"const_number_{constValue}", WireType = WireType.Constant };
    public static Wire NewConstantWire(BigInteger constValue) => new() { ConstValue = ArithConfig.FieldFactory.New(constValue), Name = $"const_number_{constValue}", WireType = WireType.Constant };
    public static Wire NewOperationResultWire(string? name, int layer) => new() { ConstValue = null, Name = name, WireType = WireType.OperationResult, Layer = layer };
}
