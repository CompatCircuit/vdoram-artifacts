using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.CompatCircuits;
using Anonymous.CompatCircuitCore.CompatCircuits.MpcCircuits;
using Anonymous.CompatCircuitCore.CompatCircuits.R1csCircuits;
using Anonymous.CompatCircuitCore.Computation;
using Anonymous.CompatCircuitCore.Computation.SingleParty;
using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives;
using System.Text;

namespace Anonymous.CompatCircuitCoreTest;

[TestClass]
public class SingleExecutorTest {
    public static CompatCircuit GetTestCircuit() {
        static MemoryStream MemoryStreamFromString(string value, Encoding encoding) => new(encoding.GetBytes(value ?? string.Empty));

        string circuitText = """
; const 0 = 0;
; const 1 = -1;
; const 2 = 1;
; const 3..254 from 2^{id-2};
; const 255 = 11; (quadratic nonresidue)
; const 256 = -11;
reserved 0 .. 256;
const 257 .. 258;
pubin 259 .. 259;
privin 260 .. 261;
total 521;

; output 265 = a * b + c * (d + e) + 4
const 257 = 114514 ; a
const 258 = 1919810 ; d
; pubin 259 ; b (input: 114)
; privin 260 ; c (input: 514)
; privin 261 ; e (input: 1919)
add 262 = 258 + 261; d + e (answer: 1921729)
mul 263 = 257 * 259 ; a * b (answer: 13054596)
mul 264 = 260 * 262 ; c * (d + e)  (answer: 987768706)
add 265 = 263 + 264; a * b + c * (d + e) (answer: 1000823302)
output 265;

; output 266 = inverse of output 265
inv 266 from 265;
output 266;

; wire 267 is not a public output
add 267 = 266 + 260;

; output 268 .. 520 = bit decompositions of 267
bits 268 .. 520 from 267;
output 268 .. 520;
""";
        using MemoryStream stream = MemoryStreamFromString(circuitText, EncodingHelper.UTF8Encoding);
        CompatCircuit circuit = CompatCircuitSerializer.Deserialize(stream);
        return circuit;
    }

    [TestMethod]
    public void TestSingleExecutor() {

        CompatCircuit circuit = GetTestCircuit();
        Dictionary<int, Field> publicInputValueDict = new() {
            {259, ArithConfig.FieldFactory.New(114)},
        };
        Dictionary<int, Field> privateInputValueDict = new() {
            {260, ArithConfig.FieldFactory.New(514)},
            {261, ArithConfig.FieldFactory.New(1919)},
        };

        CompatCircuitConverter.ToMpcCircuitAndR1csCircuit(circuit, out MpcCircuit mpcCircuit, out R1csCircuit r1csCircuit);

        SingleExecutor singleExecutor = new();
        CircuitExecuteResult executeResult = singleExecutor.Compute(CompatCircuitConverter.ToMpcCircuit(mpcCircuit), publicInputValueDict, privateInputValueDict);

        Dictionary<int, Field> outputs = executeResult.PublicOutputs;

        Dictionary<int, Field> answers = new() {
            {265,  ArithConfig.FieldFactory.New(1000823302)},
        };

        // All outputs should not be null (as the circuit is correctly written)
        foreach ((_, Field? value) in outputs) {
            Assert.IsNotNull(value);
        }

        // Verify answer for wire 265
        Assert.AreEqual(answers[265], outputs[265]);

        // Verify wire 266
        Assert.AreEqual(ArithConfig.FieldFactory.One, answers[265] * outputs[266]!);

        // Verify wire 268 .. 520
        Field sum = ArithConfig.FieldFactory.Zero;
        Field powerOfTwo = ArithConfig.FieldFactory.One;
        for (int wireID = 268; wireID <= 268 + ArithConfig.BitSize - 1; wireID++) {
            sum += powerOfTwo * outputs[wireID]!;
            powerOfTwo *= ArithConfig.FieldFactory.Two;
        }
        Assert.AreEqual(sum, outputs[266]! + privateInputValueDict[260]);

        // Check for r1cs constraints
        List<MpcValue?> valueBoard = executeResult.ValueBoard;
        Assert.IsTrue(valueBoard.All(v => v is not null && !v.IsSecretShare));
        R1csCircuitWithValues r1CsCircuitWithValues = R1csCircuitWithValues.FromR1csCircuit(r1csCircuit, executeResult.ValueBoard!);
        r1CsCircuitWithValues.SelfVerify();
    }

}
