using System.Buffers.Binary;
using System.Text;

namespace Nexus.Core;

internal static class Fnv1a64
{
    internal const ulong OffsetBasis = 14_695_981_039_346_656_037UL;
    internal const ulong Prime = 1_099_511_628_211UL;

    internal static readonly Encoding StrictUtf8 = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    internal static ulong Append(ulong hash, ReadOnlySpan<byte> bytes)
    {
        foreach (byte value in bytes)
        {
            hash ^= value;
            hash = unchecked(hash * Prime);
        }

        return hash;
    }

    internal static ulong AppendUInt32LittleEndian(ulong hash, uint value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        return Append(hash, bytes);
    }

    internal static ulong AppendUInt64LittleEndian(ulong hash, ulong value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        return Append(hash, bytes);
    }
}
