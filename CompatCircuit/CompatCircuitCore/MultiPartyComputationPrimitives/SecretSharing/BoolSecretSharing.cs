using Anonymous.CompatCircuitCore.Extensions;
using Anonymous.CompatCircuitCore.RandomGenerators;
using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.MultiPartyComputationPrimitives.SecretSharing;
/// <summary>
/// Full threshold secret sharing
/// </summary>
public class BoolSecretSharing {
    public BoolSecretSharing() { }
    public required RandomGeneratorRef RandomGenerator { get; init; }
    public List<bool> MakeShares(int partyCount, bool value) {
        if (partyCount <= 0) {
            throw new ArgumentException("Party count must be positive.", nameof(partyCount));
        }

        // Generate (partyCount - 1) random bits
        List<bool> randBits = RandomHelper.RandomBits(partyCount - 1, this.RandomGenerator).ToList();

        List<bool> shares = [];
        for (int i = 0; i < partyCount - 1; i++) {
            bool rand = randBits[i];
            value ^= rand; // XOR
            shares.Add(rand);
        }
        shares.Add(value);

        Trace.Assert(shares.Count == partyCount);
        return shares;
    }

    public bool RecoverFromShares(int partyCount, IEnumerable<bool> shares) {
        int i = 0;
        bool result = false;
        foreach (bool share in shares) {
            result ^= share; // XOR
            i++;
        }
        Trace.Assert(i == partyCount);
        return result;
    }
}
