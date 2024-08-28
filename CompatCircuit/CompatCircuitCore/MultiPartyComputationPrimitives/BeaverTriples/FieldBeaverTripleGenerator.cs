using SadPencil.CompatCircuitCore.Arithmetic;
using SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.SecretSharing;
using System.Diagnostics;

namespace SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.BeaverTriples;
public class FieldBeaverTripleGenerator {
    public required FieldFactory FieldFactory { get; init; }
    public required FieldSecretSharing FieldSecretSharing { get; init; }

    public void GenerateBeaverTripleShareFileForAllParties(IReadOnlyList<Stream> streams, int partyCount, int beaverCount, bool leaveOpen = false) {
        if (streams.Count != partyCount) {
            throw new ArgumentException("Stream count mismatch", nameof(streams));
        }

        DateTimeOffset lastDisplayTime = DateTimeOffset.Now;

        for (int i = 0; i < beaverCount; i++) {
            if (i % 10000 == 0 || i == beaverCount - 1) {
                DateTimeOffset currentTime = DateTimeOffset.Now;
                if (i != 0 && i != beaverCount - 1 && (currentTime - lastDisplayTime).TotalSeconds < 1) {
                    continue;
                }
                lastDisplayTime = currentTime;

                string percentage = ((double)(i + 1) / beaverCount * 100).ToString("F2") + "%";
                string size = ((double)streams[0].Position / 1048576).ToString("F2") + " MiB";
                Serilog.Log.Information($"[{percentage}] Generate field beaver triple ({i + 1}/{beaverCount}); {size} written");
                foreach (Stream stream in streams) {
                    stream.Flush();
                }
            }

            List<FieldBeaverTripleShare> share = this.GenerateSingleBeaverTripleShareForAllParties(partyCount);
            Trace.Assert(share.Count == partyCount);

            for (int j = 0; j < partyCount; j++) {
                FieldBeaverTripleShareFileEnumerator.AppendToStream(streams[j], share[j]);
            }
        }

        if (!leaveOpen) {
            for (int j = 0; j < partyCount; j++) {
                streams[j].Close();
            }
        }
    }

    public List<FieldBeaverTripleShare> GenerateSingleBeaverTripleShareForAllParties(int partyCount) {
        List<FieldBeaverTripleShare> ret = [];

        // Make X, Y, XY
        Field x = this.FieldFactory.Random();
        Field y = this.FieldFactory.Random();
        Field xy = x * y;

        // Make share
        List<Field> xShares = this.FieldSecretSharing.MakeShares(partyCount, x);
        List<Field> yShares = this.FieldSecretSharing.MakeShares(partyCount, y);
        List<Field> xyShares = this.FieldSecretSharing.MakeShares(partyCount, xy);

        // Append share to each party
        for (int j = 0; j < partyCount; j++) {
            ret.Add(new FieldBeaverTripleShare() {
                X = xShares[j],
                Y = yShares[j],
                XY = xyShares[j],
            });
        }

        return ret;
    }

    public List<FieldBeaverTripleShareList> GenerateBeaverTripleShareListForAllParties(int partyCount, int beaverCount) {
        List<FieldBeaverTripleShareList> beaverTripleShareListForAllParties = [];
        for (int j = 0; j < partyCount; j++) {
            beaverTripleShareListForAllParties.Add([]);
        }

        for (int i = 0; i < beaverCount; i++) {
            if (i % 10000 == 0) {
                string percentage = ((double)(i + 1) / beaverCount * 100).ToString("F2") + "%";
                Serilog.Log.Information($"[{percentage}] Generate field beaver triple ({i + 1}/{beaverCount})");
            }

            List<FieldBeaverTripleShare> share = this.GenerateSingleBeaverTripleShareForAllParties(partyCount);
            // Append share to each party
            for (int j = 0; j < partyCount; j++) {
                beaverTripleShareListForAllParties[j].Add(share[j]);
                Trace.Assert(beaverTripleShareListForAllParties[j].Count == i + 1);
            }

        }

        Trace.Assert(beaverTripleShareListForAllParties.Count == partyCount);
        for (int j = 0; j < partyCount; j++) {
            Trace.Assert(beaverTripleShareListForAllParties[j].Count == beaverCount);
        }
        return beaverTripleShareListForAllParties;
    }

    public static List<FieldBeaverTripleShareList> GenerateBeaverTripleShareListForAllParties(FieldFactory fieldFactory, int partyCount, int beaverCount) => new FieldBeaverTripleGenerator() { FieldFactory = fieldFactory, FieldSecretSharing = new FieldSecretSharing() { FieldFactory = fieldFactory } }.GenerateBeaverTripleShareListForAllParties(partyCount, beaverCount);
}
