using System.Globalization;

namespace Nexus.Core;

/// <summary>
/// Incrementally hashes typed values with FNV-1a 64 and a canonical little-endian representation.
/// </summary>
/// <remarks>
/// Each value has a type tag. Variable-length UTF-8 strings also have an unsigned 64-bit byte-length prefix.
/// Floating-point values preserve their exact IEEE 754 binary representation, including signed zero and NaN payloads.
/// </remarks>
public sealed class StableHasher64
{
    private const byte BooleanTag = 0x01;
    private const byte ByteTag = 0x02;
    private const byte Int32Tag = 0x03;
    private const byte UInt32Tag = 0x04;
    private const byte Int64Tag = 0x05;
    private const byte UInt64Tag = 0x06;
    private const byte DoubleTag = 0x07;
    private const byte StringTag = 0x08;
    private const byte SimulationTickTag = 0x09;
    private const byte EntityIdTag = 0x0A;
    private const byte DeterministicSeedTag = 0x0B;

    private ulong _value = Fnv1a64.OffsetBasis;

    /// <summary>
    /// Gets the current unsigned 64-bit hash value.
    /// </summary>
    public ulong Value => _value;

    public void Add(bool value)
    {
        AddTag(BooleanTag);
        AddRawByte(value ? (byte)1 : (byte)0);
    }

    public void Add(byte value)
    {
        AddTag(ByteTag);
        AddRawByte(value);
    }

    public void Add(int value)
    {
        AddTag(Int32Tag);
        _value = Fnv1a64.AppendUInt32LittleEndian(_value, unchecked((uint)value));
    }

    public void Add(uint value)
    {
        AddTag(UInt32Tag);
        _value = Fnv1a64.AppendUInt32LittleEndian(_value, value);
    }

    public void Add(long value)
    {
        AddTag(Int64Tag);
        _value = Fnv1a64.AppendUInt64LittleEndian(_value, unchecked((ulong)value));
    }

    public void Add(ulong value)
    {
        AddTag(UInt64Tag);
        _value = Fnv1a64.AppendUInt64LittleEndian(_value, value);
    }

    public void Add(double value)
    {
        AddTag(DoubleTag);
        _value = Fnv1a64.AppendUInt64LittleEndian(_value, BitConverter.DoubleToUInt64Bits(value));
    }

    public void Add(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        byte[] bytes = Fnv1a64.StrictUtf8.GetBytes(value);
        AddTag(StringTag);
        _value = Fnv1a64.AppendUInt64LittleEndian(_value, checked((ulong)bytes.LongLength));
        _value = Fnv1a64.Append(_value, bytes);
    }

    public void Add(SimulationTick value)
    {
        AddTag(SimulationTickTag);
        _value = Fnv1a64.AppendUInt64LittleEndian(_value, value.Value);
    }

    public void Add(EntityId value)
    {
        AddTag(EntityIdTag);
        _value = Fnv1a64.AppendUInt64LittleEndian(_value, value.Value);
    }

    public void Add(DeterministicSeed value)
    {
        AddTag(DeterministicSeedTag);
        _value = Fnv1a64.AppendUInt64LittleEndian(_value, value.Value);
    }

    /// <summary>
    /// Returns the canonical uppercase, zero-padded 16-character hexadecimal representation.
    /// </summary>
    public string ToHexString() => _value.ToString("X16", CultureInfo.InvariantCulture);

    public override string ToString() => ToHexString();

    private void AddTag(byte tag) => AddRawByte(tag);

    private void AddRawByte(byte value)
    {
        Span<byte> bytes = stackalloc byte[1];
        bytes[0] = value;
        _value = Fnv1a64.Append(_value, bytes);
    }
}
