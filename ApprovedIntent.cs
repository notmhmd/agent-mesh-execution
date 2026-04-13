using System.Text.Json.Serialization;

namespace Execution.Gateway;

public sealed record ApprovedIntentV1(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("intent_id")] string? IntentId,
    [property: JsonPropertyName("trace_id")] string? TraceId,
    [property: JsonPropertyName("symbol")] string? Symbol,
    [property: JsonPropertyName("side")] string? Side,
    [property: JsonPropertyName("qty")] int Qty,
    [property: JsonPropertyName("idempotency_key")] string? IdempotencyKey,
    [property: JsonPropertyName("environment")] string? Environment,
    [property: JsonPropertyName("created_at_unix")] double CreatedAtUnix);

[JsonSerializable(typeof(ApprovedIntentV1))]
internal partial class ExecutionJsonContext : JsonSerializerContext;
