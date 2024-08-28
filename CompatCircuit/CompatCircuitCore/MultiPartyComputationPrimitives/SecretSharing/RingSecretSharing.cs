using SadPencil.CompatCircuitCore.Arithmetic;
using System.Diagnostics;

namespace SadPencil.CompatCircuitCore.MultiPartyComputationPrimitives.SecretSharing;
/// <summary>
/// Full threshold secret sharing
/// </summary>
public class RingSecretSharing {
    public required RingFactory RingFactory { get; init; }
    public List<Ring> MakeShares(int partyCount, Ring value) {
        if (partyCount <= 0) {
            throw new ArgumentException("Party count must be positive.", nameof(partyCount));
        }

        List<Ring> shares = [];
        for (int i = 0; i < partyCount - 1; i++) {
            Ring rand = this.RingFactory.Random();
            value -= rand;
            shares.Add(rand);
        }
        shares.Add(value);

        Trace.Assert(shares.Count == partyCount);
        return shares;
    }

    public Ring RecoverFromShares(int partyCount, IEnumerable<Ring> shares) {
        int i = 0;
        Ring result = this.RingFactory.Zero;
        foreach (Ring share in shares) {
            result += share;
            i++;
        }
        Trace.Assert(i == partyCount);
        return result;
    }
}