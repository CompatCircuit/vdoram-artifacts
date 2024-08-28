using SadPencil.CompatCircuitCore.Extensions;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.SecretSharing;
using SadPencil.CompatCircuitCore.RandomGenerators;
using System.Diagnostics;

namespace SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
public class BoolBeaverTripleGenerator {
    public required RandomGeneratorRef RandomGenerator { get; init; }
    public required BoolSecretSharing BoolSecretSharing { get; init; }
    public void GenerateBeaverTripleShareFileForAllParties(IReadOnlyList<Stream> streams, int partyCount, int beaverCount, bool leaveOpen = false) {
        if (streams.Count != partyCount) {
            throw new ArgumentException("Stream count mismatch", nameof(streams));
        }

        DateTimeOffset lastDisplayTime = DateTimeOffset.Now;

        for (int i = 0; i < beaverCount; i++) {
            if (i % 50000 == 0 || i == beaverCount - 1) {
                DateTimeOffset currentTime = DateTimeOffset.Now;
                if (i != 0 && i != beaverCount - 1 && (currentTime - lastDisplayTime).TotalSeconds < 1) {
                    continue;
                }
                lastDisplayTime = currentTime;

                string percentage = ((double)(i + 1) / beaverCount * 100).ToString("F2") + "%";
                string size = ((double)streams[0].Position / 1048576).ToString("F2") + " MiB";
                Serilog.Log.Information($"[{percentage}] Generate bool beaver triple ({i + 1}/{beaverCount}); {size} written");
                foreach (Stream stream in streams) {
                    stream.Flush();
                }
            }

            List<BoolBeaverTripleShare> share = this.GenerateSingleBeaverTripleShareForAllParties(partyCount);
            Trace.Assert(share.Count == partyCount);

            for (int j = 0; j < partyCount; j++) {
                BoolBeaverTripleShareFileEnumerator.AppendToStream(streams[j], share[j]);
            }
        }

        if (!leaveOpen) {
            for (int j = 0; j < partyCount; j++) {
                streams[j].Close();
            }
        }
    }

    public List<BoolBeaverTripleShare> GenerateSingleBeaverTripleShareForAllParties(int partyCount) {
        List<BoolBeaverTripleShare> ret = [];

        List<bool> randomBits = RandomHelper.RandomBits(2, this.RandomGenerator).ToList();
        // Make X, Y, XY
        bool x = randomBits[0];
        bool y = randomBits[1];
        bool xy = x & y; // AND

        // Make shares
        List<bool> xShares = this.BoolSecretSharing.MakeShares(partyCount, x);
        List<bool> yShares = this.BoolSecretSharing.MakeShares(partyCount, y);
        List<bool> xyShares = this.BoolSecretSharing.MakeShares(partyCount, xy);

        // Append shares to each party
        for (int j = 0; j < partyCount; j++) {
            ret.Add(new BoolBeaverTripleShare() {
                X = xShares[j],
                Y = yShares[j],
                XY = xyShares[j],
            });
        }

        return ret;
    }

    public List<BoolBeaverTripleShareList> GenerateBeaverTripleShareListForAllParties(int partyCount, int beaverCount) {
        List<BoolBeaverTripleShareList> beaverTripleShareListForAllParties = [];
        for (int j = 0; j < partyCount; j++) {
            beaverTripleShareListForAllParties.Add([]);
        }

        List<bool> randomBits = RandomHelper.RandomBits(2 * beaverCount, this.RandomGenerator).ToList();
        for (int i = 0; i < beaverCount; i++) {
            if (i % 10000 == 0) {
                string percentage = ((double)(i + 1) / beaverCount * 100).ToString("F2") + "%";
                Serilog.Log.Information($"[{percentage}] Generate bool beaver triple ({i + 1}/{beaverCount})");
            }

            // Make X, Y, XY
            bool x = randomBits[2 * i];
            bool y = randomBits[(2 * i) + 1];
            bool xy = x & y; // AND

            // Make shares
            List<bool> xShares = this.BoolSecretSharing.MakeShares(partyCount, x);
            List<bool> yShares = this.BoolSecretSharing.MakeShares(partyCount, y);
            List<bool> xyShares = this.BoolSecretSharing.MakeShares(partyCount, xy);

            // Append shares to each party
            for (int j = 0; j < partyCount; j++) {
                beaverTripleShareListForAllParties[j].Add(new BoolBeaverTripleShare() {
                    X = xShares[j],
                    Y = yShares[j],
                    XY = xyShares[j],
                });

                Trace.Assert(beaverTripleShareListForAllParties[j].Count == i + 1);
            }

        }

        Trace.Assert(beaverTripleShareListForAllParties.Count == partyCount);
        for (int j = 0; j < partyCount; j++) {
            Trace.Assert(beaverTripleShareListForAllParties[j].Count == beaverCount);
        }
        return beaverTripleShareListForAllParties;
    }

    public static List<BoolBeaverTripleShareList> GenerateBeaverTripleShareListForAllParties(BoolSecretSharing boolSecretSharing, RandomGeneratorRef randomGenerator, int partyCount, int beaverCount) =>
        new BoolBeaverTripleGenerator() { BoolSecretSharing = boolSecretSharing, RandomGenerator = randomGenerator }.GenerateBeaverTripleShareListForAllParties(partyCount, beaverCount);
}
