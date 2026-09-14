using System;

namespace Nexus.Gameplay.World
{
    // These DTOs intentionally mirror the small, versioned JSON envelope in
    // Nexus.Simulation. They contain no Unity objects or runtime identities.
    [Serializable]
    public sealed class DistrictAuthorityWireMessage
    {
        public string protocolVersion;
        public string messageType;
        public string sessionId;
        public string clientId;
        public ulong clientSequence;
        public string commandId;
        public string commandType;
        public string entityId;
        public ulong targetTick;
        public ulong seed;
        public string campaignId;
        public DistrictAuthorityWireEvent @event;
        public DistrictAuthorityWireSnapshot snapshot;
        public string errorCode;
        public string errorMessage;
    }

    [Serializable]
    public sealed class DistrictAuthorityWireEvent
    {
        public string protocolVersion;
        public string sessionId;
        public ulong serverSequence;
        public ulong simulationTick;
        public string type;
        public string entityId;
        public string value;
    }

    [Serializable]
    public sealed class DistrictAuthorityWireSnapshot
    {
        public string sessionId;
        public ulong seed;
        public ulong tick;
        public ulong serverSequence;
        public string phase;
        public string crisis1;
        public string crisis2;
        public string infrastructure;
        public string route;
        public bool crisis2InheritedDamage;
        public ulong crisis2DeadlineTick;
        public DistrictAuthorityWireZone[] zones;
        public string campaignId;
        public ulong saveRevision;
        public string saveStatus;
        public string saveFormatVersion;
        public DistrictAuthorityWireMemory[] memories;
        public string stateHash;
    }

    [Serializable]
    public sealed class DistrictAuthorityWireZone
    {
        public string id;
        public bool visited;
        public string civilianState;
    }

    [Serializable]
    public sealed class DistrictAuthorityWireMemory
    {
        public string memoryId;
        public ulong simulationTick;
        public string districtId;
        public string kind;
        public string subjectId;
        public string value;
    }
}
