namespace Anonymous.CompatCircuitCore.Computation.MultiParty.Messages;
public enum MessagePayloadType : byte {
    PartyOnline = 1,
    PartyCompleted = 2,
    BigIntegerExposure = 3,
    BigIntegerInputShare = 4,
    BoolExposure = 5,
}
