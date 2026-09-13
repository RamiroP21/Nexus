using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexus.Simulation;

public static class DistrictMessageTypes
{
    public const string ClientHello = "ClientHello";
    public const string ServerHello = "ServerHello";
    public const string CreateSession = "CreateSession";
    public const string SessionStarted = "SessionStarted";
    public const string ResumeSession = "ResumeSession";
    public const string SnapshotRequest = "SnapshotRequest";
    public const string Snapshot = "Snapshot";
    public const string ClientCommand = "ClientCommand";
    public const string ServerEvent = "ServerEvent";
    public const string CommandRejected = "CommandRejected";
    public const string ProtocolError = "ProtocolError";
    public const string Ping = "Ping";
    public const string Pong = "Pong";
}

public sealed record DistrictWireMessage
{
    public string ProtocolVersion { get; init; } = DistrictAuthorityProtocol.Version;
    public string MessageType { get; init; } = "";
    public string? SessionId { get; init; }
    public string? ClientId { get; init; }
    public ulong ClientSequence { get; init; }
    public string? CommandId { get; init; }
    public DistrictCommandType? CommandType { get; init; }
    public string? EntityId { get; init; }
    public ulong? TargetTick { get; init; }
    public ulong Seed { get; init; }
    public DistrictEvent? Event { get; init; }
    public DistrictSnapshot? Snapshot { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

public static class DistrictProtocolCodec
{
    public const int MaxFrameBytes = 131_072;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new NullableCommandTypeConverter(), new NullableEventConverter(), new NullableSnapshotConverter(), new JsonStringEnumConverter() },
        WriteIndented = false
    };
    // Concrete payloads must be serialized without the nullable-wrapper converters;
    // nullable annotations erase at runtime and would otherwise re-enter Write.
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false
    };

    private sealed class NullableCommandTypeConverter : JsonConverter<DistrictCommandType?>
    {
        public override DistrictCommandType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.String) throw new JsonException("CommandType must be a string or null.");
            string? value = reader.GetString();
            return string.IsNullOrEmpty(value) ? null : Enum.Parse<DistrictCommandType>(value, true);
        }

        public override void Write(Utf8JsonWriter writer, DistrictCommandType? value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value?.ToString());
    }

    private sealed class NullableEventConverter : JsonConverter<DistrictEvent?>
    {
        public override DistrictEvent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("type", out JsonElement type)
                || string.IsNullOrEmpty(type.GetString())) return null;
            return JsonSerializer.Deserialize<DistrictEvent>(document.RootElement.GetRawText(), PayloadJsonOptions);
        }

        public override void Write(Utf8JsonWriter writer, DistrictEvent? value, JsonSerializerOptions options)
        {
            if (value is null) writer.WriteNullValue();
            else JsonSerializer.Serialize(writer, value, PayloadJsonOptions);
        }
    }

    private sealed class NullableSnapshotConverter : JsonConverter<DistrictSnapshot?>
    {
        public override DistrictSnapshot? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("sessionId", out JsonElement session)
                || string.IsNullOrEmpty(session.GetString())) return null;
            return JsonSerializer.Deserialize<DistrictSnapshot>(document.RootElement.GetRawText(), PayloadJsonOptions);
        }

        public override void Write(Utf8JsonWriter writer, DistrictSnapshot? value, JsonSerializerOptions options)
        {
            if (value is null) writer.WriteNullValue();
            else JsonSerializer.Serialize(writer, value, PayloadJsonOptions);
        }
    }

    public static byte[] Serialize(DistrictWireMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
    }

    public static DistrictWireMessage Deserialize(ReadOnlySpan<byte> payload)
    {
        try
        {
            return JsonSerializer.Deserialize<DistrictWireMessage>(payload, JsonOptions)
                ?? throw new InvalidDataException("Empty protocol message.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Malformed protocol JSON.", ex);
        }
    }

    public static async ValueTask WriteAsync(Stream stream, DistrictWireMessage message, CancellationToken cancellationToken = default)
    {
        byte[] payload = Serialize(message);
        if (payload.Length > MaxFrameBytes) throw new InvalidDataException("Protocol frame is too large.");
        byte[] header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);
        await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<DistrictWireMessage?> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        byte[] header = new byte[sizeof(int)];
        if (!await ReadExactlyAsync(stream, header, cancellationToken).ConfigureAwait(false)) return null;
        int length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length <= 0 || length > MaxFrameBytes) throw new InvalidDataException("Invalid protocol frame length.");
        byte[] payload = new byte[length];
        if (!await ReadExactlyAsync(stream, payload, cancellationToken).ConfigureAwait(false)) throw new EndOfStreamException();
        return Deserialize(payload);
    }

    private static async ValueTask<bool> ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[offset..], cancellationToken).ConfigureAwait(false);
            if (read == 0) return offset == 0;
            offset += read;
        }
        return true;
    }
}
