using Anonymous.CompatCircuitCore.Arithmetic;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.SecretSharing;
/// <summary>
/// Full threshold secret sharing
/// </summary>
public class FieldSecretSharing {
    public required FieldFactory FieldFactory { get; init; }
    public List<Field> MakeShares(int partyCount, Field value) {
        if (partyCount <= 0) {
            throw new ArgumentException("Party count must be positive.", nameof(partyCount));
        }

        List<Field> shares = [];
        for (int i = 0; i < partyCount - 1; i++) {
            Field rand = this.FieldFactory.Random();
            value -= rand;
            shares.Add(rand);
        }
        shares.Add(value);

        Trace.Assert(shares.Count == partyCount);
        return shares;
    }

    public Field RecoverFromShares(int partyCount, IEnumerable<Field> shares) {
        int i = 0;
        Field result = this.FieldFactory.Zero;
        foreach (Field share in shares) {
            result += share;
            i++;
        }
        Trace.Assert(i == partyCount);
        return result;
    }
}
