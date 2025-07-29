using Anonymous.CompatCircuitCore.Arithmetic;
using Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.SecretSharing;
using Anonymous.CompatCircuitCore.RandomGenerators;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.EdaBitsKai;
public class EdaBitsKaiGenerator {
    public FieldFactory BaseFieldFactory { get; }
    public FieldSecretSharing BaseFieldSecretSharing { get; }
    public RandomGeneratorRef RandomGenerator { get; }
    public BoolSecretSharing BoolSecretSharing { get; }
    public int PartyCount { get; }

    // TODO: some parameters are not used. They can be removed.
    public EdaBitsKaiGenerator(int ringBitSize, int partyCount, FieldFactory fieldFactory, FieldSecretSharing fieldSecretSharing, BoolSecretSharing boolSecretSharing, RandomGeneratorRef randomGenerator) {
        this.BaseFieldFactory = fieldFactory;
        this.BaseFieldSecretSharing = fieldSecretSharing;
        this.PartyCount = partyCount;
        this.BoolSecretSharing = boolSecretSharing;
        this.RandomGenerator = randomGenerator;
    }

    public void GenerateEdaBitsShareFileForAllParties(IReadOnlyList<Stream> streams, int edaBitsCount, bool leaveOpen = false) {
        if (streams.Count != this.PartyCount) {
            throw new ArgumentException("Stream count mismatch", nameof(streams));
        }

        DateTimeOffset lastDisplayTime = DateTimeOffset.Now;

        for (int i = 0; i < edaBitsCount; i++) {
            if (i % 10000 == 0 || i == edaBitsCount - 1) {
                DateTimeOffset currentTime = DateTimeOffset.Now;
                if (i != 0 && i != edaBitsCount - 1 && (currentTime - lastDisplayTime).TotalSeconds < 1) {
                    continue;
                }
                lastDisplayTime = currentTime;

                string percentage = ((double)(i + 1) / edaBitsCount * 100).ToString("F2") + "%";
                string size = ((double)streams[0].Position / 1048576).ToString("F2") + " MiB";
                Serilog.Log.Information($"[{percentage}] Generate edaBitsKai triple ({i + 1}/{edaBitsCount}); {size} written");
                foreach (Stream stream in streams) {
                    stream.Flush();
                }
            }

            List<EdaBitsKaiShare> share = this.GenerateSingleEdaBitsShareForAllParties();
            Trace.Assert(share.Count == this.PartyCount);

            for (int j = 0; j < this.PartyCount; j++) {
                EdaBitsKaiShareFileEnumerator.AppendToStream(streams[j], share[j]);
            }
        }

        if (!leaveOpen) {
            for (int j = 0; j < this.PartyCount; j++) {
                streams[j].Close();
            }
        }
    }

    private List<EdaBitsKaiShare> GenerateSingleEdaBitsShareForAllParties() {
        List<EdaBitsKaiShare> ret = [];

        Field arithValue = this.BaseFieldFactory.Random();
        List<Field> arithShares = this.BaseFieldSecretSharing.MakeShares(this.PartyCount, arithValue);

        List<List<bool>> bitsShares = arithValue.BitDecomposition().Select(bit => this.BoolSecretSharing.MakeShares(this.PartyCount, bit)).ToList();

        for (int j = 0; j < this.PartyCount; j++) {
            List<bool> bitsSharesForOneParty = bitsShares.Select(list => list[j]).ToList();
            ret.Add(new EdaBitsKaiShare() { ArithShare = arithShares[j], BoolShares = bitsSharesForOneParty });
        }

        return ret;
    }

    public static List<EdaBitsKaiShareList> GenerateEdaBitsShareListForAllParties(FieldFactory fieldFactory, FieldSecretSharing fieldSecretSharing, BoolSecretSharing boolSecretSharing, RandomGeneratorRef randomGenerator, int ringBitSize, int partyCount, int edaBitsCount) =>
        new EdaBitsKaiGenerator(ringBitSize, partyCount, fieldFactory, fieldSecretSharing, boolSecretSharing, randomGenerator).GenerateEdaBitsShareListForAllParties(edaBitsCount);

    public List<EdaBitsKaiShareList> GenerateEdaBitsShareListForAllParties(int edaBitsCount) {
        List<EdaBitsKaiShareList> edaBitsShareListForAllParties = [];
        for (int j = 0; j < this.PartyCount; j++) {
            edaBitsShareListForAllParties.Add([]);
        }

        for (int i = 0; i < edaBitsCount; i++) {
            if (i % 10000 == 0) {
                string percentage = ((double)(i + 1) / edaBitsCount * 100).ToString("F2") + "%";
                Serilog.Log.Information($"[{percentage}] Generate edaBitsKai triple ({i + 1}/{edaBitsCount})");
            }

            List<EdaBitsKaiShare> share = this.GenerateSingleEdaBitsShareForAllParties();

            // Append share to each party
            for (int j = 0; j < this.PartyCount; j++) {
                edaBitsShareListForAllParties[j].Add(share[j]);
            }
        }

        return edaBitsShareListForAllParties;
    }
}