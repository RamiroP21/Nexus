using System;

namespace Nexus.Persistence01
{
    public enum RescueOutcome { Unknown = 0, Protected = 1, Hit = 2 }
    public enum DepotOutcome { Unknown = 0, Contained = 1, Overrun = 2 }

    // EXPERIMENTAL / LOCAL TO PERSISTENCE01. Facts, not a choice or a physics snapshot.
    [Serializable]
    public sealed class Persistence01State
    {
        public int version;
        public RescueOutcome rescue;
        public DepotOutcome depot;
        public int lostGround;

        public bool IsValid => version == 1
            && (rescue == RescueOutcome.Protected || rescue == RescueOutcome.Hit)
            && (depot == DepotOutcome.Contained || depot == DepotOutcome.Overrun)
            && lostGround >= 0 && lostGround <= 3
            && (depot != DepotOutcome.Overrun || lostGround == 3);
    }
}
