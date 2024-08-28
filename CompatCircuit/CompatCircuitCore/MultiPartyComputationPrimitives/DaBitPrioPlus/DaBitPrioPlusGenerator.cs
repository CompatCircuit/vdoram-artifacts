using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.Extensions;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.SecretSharing;
using SadPencil.CompatCircuitCore.RandomGenerators;
using System.Diagnostics;

namespace SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.DaBitPrioPlus;
public class DaBitPrioPlusGenerator {
    public required FieldFactory FieldFactory { get; init; }
    public required FieldSecretSharing FieldSecretSharing { get; init; }
    public required RandomGeneratorRef RandomGenerator { get; init; }
    public required BoolSecretSharing BoolSecretSharing { get; init; }

    public void GenerateDaBitPrioPlusShareFileForAllParties(IReadOnlyList<Stream> streams, int partyCount, int daBitPrioPlusCount, bool leaveOpen = false) {
        if (streams.Count != partyCount) {
            throw new ArgumentException("Stream count mismatch", nameof(streams));
        }

        DateTimeOffset lastDisplayTime = DateTimeOffset.Now;

        for (int i = 0; i < daBitPrioPlusCount; i++) {
            if (i % 10000 == 0 || i == daBitPrioPlusCount - 1) {
                DateTimeOffset currentTime = DateTimeOffset.Now;
                if (i != 0 && i != daBitPrioPlusCount - 1 && (currentTime - lastDisplayTime).TotalSeconds < 1) {
                    continue;
                }
                lastDisplayTime = currentTime;

                string percentage = ((double)(i + 1) / daBitPrioPlusCount * 100).ToString("F2") + "%";
                string size = ((double)streams[0].Position / 1048576).ToString("F2") + " MiB";
                Serilog.Log.Information($"[{percentage}] Generate daBitPrioPlus triple ({i + 1}/{daBitPrioPlusCount}); {size} written");
                foreach (Stream stream in streams) {
                    stream.Flush();
                }
            }

            List<DaBitPrioPlusShare> share = this.GenerateSingleDaBitPrioPlusShareForAllParties(partyCount);
            Trace.Assert(share.Count == partyCount);

            for (int j = 0; j < partyCount; j++) {
                DaBitPrioPlusShareFileEnumerator.AppendToStream(streams[j], share[j]);
            }
        }

        if (!leaveOpen) {
            for (int j = 0; j < partyCount; j++) {
                streams[j].Close();
            }
        }
    }

    public List<DaBitPrioPlusShare> GenerateSingleDaBitPrioPlusShareForAllParties(int partyCount) {
        List<DaBitPrioPlusShare> ret = [];

        bool randomBit = RandomHelper.RandomBits(1, this.RandomGenerator)[0];

        Field arithValue = randomBit ? this.FieldFactory.One : this.FieldFactory.Zero;
        List<Field> arithShares = this.FieldSecretSharing.MakeShares(partyCount, arithValue);
        List<bool> boolShares = this.BoolSecretSharing.MakeShares(partyCount, randomBit);

        for (int partyIndex = 0; partyIndex < partyCount; partyIndex++) {
            ret.Add(new DaBitPrioPlusShare() { ArithShare = arithShares[partyIndex], BoolShare = boolShares[partyIndex] });
        }

        return ret;
    }

    public List<DaBitPrioPlusShareList> GenerateDaBitPrioPlusShareListForAllParties(int partyCount, int daBitPrioPlusCount) {
        List<DaBitPrioPlusShareList> daBitPrioPlusShareListForAllParties = [];
        for (int j = 0; j < partyCount; j++) {
            daBitPrioPlusShareListForAllParties.Add([]);
        }

        List<bool> randomBits = RandomHelper.RandomBits(daBitPrioPlusCount, this.RandomGenerator).ToList();

        for (int i = 0; i < daBitPrioPlusCount; i++) {
            if (i % 10000 == 0) {
                string percentage = ((double)(i + 1) / daBitPrioPlusCount * 100).ToString("F2") + "%";
                Serilog.Log.Information($"[{percentage}] Generate daBitPrioPlus triple ({i + 1}/{daBitPrioPlusCount})");
            }

            bool randomBit = randomBits[i];
            Field arithValue = randomBit ? this.FieldFactory.One : this.FieldFactory.Zero;
            List<Field> arithShares = this.FieldSecretSharing.MakeShares(partyCount, arithValue);
            List<bool> boolShares = this.BoolSecretSharing.MakeShares(partyCount, randomBit);

            for (int partyIndex = 0; partyIndex < partyCount; partyIndex++) {
                daBitPrioPlusShareListForAllParties[partyIndex].Add(new DaBitPrioPlusShare() { ArithShare = arithShares[partyIndex], BoolShare = boolShares[partyIndex] });
            }
        }

        return daBitPrioPlusShareListForAllParties;
    }

    public static List<DaBitPrioPlusShareList> GenerateDaBitPrioPlusShareListForAllParties(FieldFactory fieldFactory, BoolSecretSharing boolSecretSharing, RandomGeneratorRef randomGenerator, int partyCount, int daBitPrioPlusCount) =>
        new DaBitPrioPlusGenerator() {
            FieldFactory = fieldFactory,
            FieldSecretSharing = new FieldSecretSharing() { FieldFactory = fieldFactory },
            BoolSecretSharing = boolSecretSharing,
            RandomGenerator = randomGenerator,
        }.GenerateDaBitPrioPlusShareListForAllParties(partyCount, daBitPrioPlusCount);
}
