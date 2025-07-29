using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.GlobalConfig;
using Anonymous.CompatCircuitProgramming.CircuitElements;

namespace Anonymous.CompatCircuitProgramming.Gadgets;
public class MimcEncryptGadget(IEnumerable<Field> roundConstants, int sBox) : IGadget {
    public int SBox { get; } = sBox; // e.g., 7
    public Field SBoxField { get; } = ArithConfig.FieldFactory.New(sBox);
    public int RoundCount => this.RoundConstants.Count; // ceil(log(q)/log(sBox))
    public IReadOnlyList<Field> RoundConstants { get; } = roundConstants.ToList(); // randomly (e.g. via SHA-3) chosen; hardcorded into implementation

    private static readonly List<string> DefaultRoundConstantStrValues = ["0", "42", "43", "170", "2209", "16426", "78087", "279978", "823517", "2097194", "4782931", "10000042", "19487209", "35831850", "62748495", "105413546", "170859333", "268435498", "410338651", "612220074", "893871697", "1280000042", "1801088567", "2494357930", "3404825421", "4586471466", "6103515587", "8031810218", "10460353177", "13492928554", "17249876351", "21870000042", "27512614133", "34359738410", "42618442955", "52523350186", "64339296833", "78364164138", "94931877159", "114415582634", "137231006717", "163840000042", "194754273907", "230539333290", "271818611081", "319277809706", "373669453167", "435817657258", "506623120485", "587068342314", "678223072891", "781250000042", "897410677873", "1028071702570", "1174711139799", "1338925210026", "1522435234413", "1727094849578", "1954897493219", "2207984167594", "2488651484857", "2799360000042", "3142742835999", "3521614606250", "3938980639125"];
    private static readonly List<Field> DefaultRoundConstants = DefaultRoundConstantStrValues.Select(ArithConfig.FieldFactory.FromString).ToList();
    private static readonly int DefaultSBox = 7;

    public static MimcEncryptGadget GetGadgetWithDefaultParams() => new(DefaultRoundConstants, DefaultSBox);

    public GadgetInstance ApplyGadget(IReadOnlyList<Wire> inputWires, string namePrefix) {
        if (inputWires.Count != 2) {
            throw new ArgumentException("Unexpected element count", nameof(inputWires));
        }

        GadgetIncompleteCircuitBoard circuitBoard = new();

        Wire plaintext = inputWires[0];
        Wire key = inputWires[1];

        // Add round constants
        List<Wire> roundConstantWires = [];
        for (int i = 0; i < this.RoundCount; i++) {
            Wire roundConstantWire = Wire.NewConstantWire(this.RoundConstants[i], $"{namePrefix}_[mimc_enc]_round_const_{i}");
            circuitBoard.AddNewConstantWire(roundConstantWire);
            roundConstantWires.Add(roundConstantWire);
        }

        // https://github.com/jwasinger/zokrates-mimc/blob/master/mimc.code

        Wire x = plaintext;
        for (int i = 0; i < this.RoundCount; i++) {
            // t1 = x + k + c
            GadgetInstance ins1 = new FieldAddGadget(3).ApplyGadget([x, key, roundConstantWires[i]], $"{namePrefix}_[mimc_enc]_{i}_add()");
            ins1.Save(circuitBoard);

            // t2 = t1 ^ sBox
            GadgetInstance ins2 = new FieldConstPowGadget(this.SBoxField).ApplyGadget([ins1.OutputWires[0]], $"{namePrefix}_[mimc_enc]_{i}_pow()");
            ins2.Save(circuitBoard);

            // x = t2
            x = ins2.OutputWires[0];
        }

        // return x + k
        GadgetInstance ins3 = new FieldAddGadget().ApplyGadget([x, key], $"{namePrefix}_[mimc_enc]_final_add()");
        ins3.Save(circuitBoard);
        Wire output = ins3.OutputWires[0];

        return GadgetIncompleteCircuitBoardHelper.NewGadgetInstanceFromGadgetIncompleteCircuitBoard([output], circuitBoard);
    }
    public List<string> GetInputWireNames() => ["plaintext", "key"];
    public List<string> GetOutputWireNames() => ["ciphertext"];
}
