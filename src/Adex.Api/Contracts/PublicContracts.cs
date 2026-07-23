using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adex.Api.Contracts;

/// <summary>
/// Transport DTOs for the public v1 surface.
///
/// These are separate types from the domain on purpose: no database entity and
/// no domain record is ever serialized to a client (ADR-0006). Property names
/// are declared explicitly rather than relying on a naming convention, so the
/// wire format is visible in the source and cannot change by reconfiguration.
///
/// The authority for these shapes is
/// <c>packages/contracts/openapi/adex-public-v1.yaml</c>; the contract tests
/// assert that the running API still matches it.
/// </summary>
public sealed record EligibleAlternativeDto(
    [property: JsonPropertyName("key")] string? Key);

public sealed record DecisionRequestDto(
    [property: JsonPropertyName("placement")] string? Placement,
    [property: JsonPropertyName("eligible_alternatives")] IReadOnlyList<EligibleAlternativeDto>? EligibleAlternatives,
    [property: JsonPropertyName("subject_id")] string? SubjectId = null,
    [property: JsonPropertyName("context")] IReadOnlyDictionary<string, JsonElement>? Context = null);

public sealed record PolicyReferenceDto(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("version")] int Version);

public sealed record DecisionResponseDto(
    [property: JsonPropertyName("decision_id")] string DecisionId,
    [property: JsonPropertyName("alternative_key")] string AlternativeKey,
    [property: JsonPropertyName("policy")] PolicyReferenceDto Policy,
    [property: JsonPropertyName("decided_at")] string DecidedAt);

public sealed record EventRequestDto(
    [property: JsonPropertyName("event_id")] string? EventId,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("occurred_at")] string? OccurredAt,
    [property: JsonPropertyName("decision_id")] string? DecisionId = null,
    [property: JsonPropertyName("subject_id")] string? SubjectId = null,
    [property: JsonPropertyName("properties")] IReadOnlyDictionary<string, JsonElement>? Properties = null);

public sealed record EventResponseDto(
    [property: JsonPropertyName("event_id")] string EventId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("received_at")] string ReceivedAt);

public sealed record HealthCheckDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("required")] bool Required,
    [property: JsonPropertyName("detail")] string? Detail);

public sealed record HealthReportDto(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("checks")] IReadOnlyList<HealthCheckDto> Checks);

/// <summary>One field-level validation failure inside a problem+json response.</summary>
public sealed record ValidationErrorDto(
    [property: JsonPropertyName("pointer")] string Pointer,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("detail")] string? Detail = null);
