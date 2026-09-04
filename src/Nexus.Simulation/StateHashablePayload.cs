using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.Simulation;

internal static class StateHashablePayload
{
    public static string CaptureStableTypeId(IStateHashable payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        string stableTypeId = payload.StableTypeId;
        if (string.IsNullOrWhiteSpace(stableTypeId))
        {
            throw new InvalidOperationException(
                "A deterministic payload requires a non-empty stable type ID.");
        }

        return stableTypeId;
    }

    public static ulong ComputeHash(
        string formatVersion,
        string stableTypeId,
        IStateHashable payload)
    {
        var payloadHasher = new StableHasher64();
        payloadHasher.Add(formatVersion);
        payloadHasher.Add(stableTypeId);
        payload.ContributeToHash(payloadHasher);
        return payloadHasher.Value;
    }
}
