using System.Globalization;
using System.Text.Json;
using Adex.Api.Contracts;
using Adex.Domain.Alternatives;
using Adex.Domain.Decisions;
using Adex.Domain.Events;
using Adex.Domain.Identifiers;
using Adex.Domain.Placements;

// ASP.NET Core brings Microsoft.Extensions.Logging.EventId into scope through
// implicit usings; this alias keeps the domain type unambiguous.
using EventId = Adex.Domain.Identifiers.EventId;

namespace Adex.Api.Validation;

/// <summary>
/// Boundary validation for the public v1 payloads.
///
/// Two rules that look strict on purpose:
/// <list type="bullet">
///   <item>unknown keys are rejected rather than ignored, so a tenant finds out
///   that their page and their configuration disagree;</item>
///   <item>timestamps must carry an explicit <c>Z</c>; any other offset is
///   rejected rather than converted, so no stored value is ambiguous about which
///   clock produced it.</item>
/// </list>
/// Every failure carries a JSON Pointer and a machine-readable code, because a
/// developer integrating the SDK reads these in a browser network panel.
/// </summary>
public static class RequestValidation
{
    public const int MaxEligibleAlternatives = 50;
    public const int MaxContextKeys = DecisionContext.MaxKeys;
    public const int MaxPropertyKeys = 64;
    public const int MaxValueLength = DecisionContext.MaxValueLength;

    public sealed record DecisionInput(
        PlacementKey Placement,
        IReadOnlyList<AlternativeKey> Eligible,
        DecisionContext Context,
        SubjectId? Subject);

    public sealed record EventInput(
        EventId EventId,
        EventType Type,
        DateTimeOffset OccurredAt,
        DecisionId? Decision,
        SubjectId? Subject,
        IReadOnlyDictionary<string, string?> Properties);

    public static bool TryValidate(
        DecisionRequestDto? request,
        out DecisionInput input,
        out List<ValidationErrorDto> errors)
    {
        errors = [];
        input = null!;

        if (request is null)
        {
            errors.Add(new ValidationErrorDto("", "required", "A JSON body is required."));
            return false;
        }

        if (!PlacementKey.TryParse(request.Placement, out PlacementKey placement))
        {
            errors.Add(new ValidationErrorDto(
                "/placement",
                request.Placement is null ? "required" : "pattern",
                "Placement must match ^[a-z0-9]([a-z0-9._-]{0,62}[a-z0-9])?$."));
        }

        List<AlternativeKey> eligible = [];
        IReadOnlyList<EligibleAlternativeDto>? alternatives = request.EligibleAlternatives;
        if (alternatives is null || alternatives.Count == 0)
        {
            errors.Add(new ValidationErrorDto(
                "/eligible_alternatives",
                alternatives is null ? "required" : "min_items",
                "At least one eligible alternative is required."));
        }
        else if (alternatives.Count > MaxEligibleAlternatives)
        {
            errors.Add(new ValidationErrorDto(
                "/eligible_alternatives",
                "max_items",
                $"At most {MaxEligibleAlternatives} eligible alternatives are accepted."));
        }
        else
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < alternatives.Count; index++)
            {
                string pointer = $"/eligible_alternatives/{index.ToString(CultureInfo.InvariantCulture)}/key";
                string? key = alternatives[index].Key;

                if (!AlternativeKey.TryParse(key, out AlternativeKey alternativeKey))
                {
                    errors.Add(new ValidationErrorDto(
                        pointer,
                        key is null ? "required" : "pattern",
                        "Alternative keys must match ^[a-z0-9]([a-z0-9._-]{0,62}[a-z0-9])?$."));
                    continue;
                }

                if (!seen.Add(alternativeKey.Value))
                {
                    errors.Add(new ValidationErrorDto(pointer, "duplicate", "Alternative keys must be unique."));
                    continue;
                }

                eligible.Add(alternativeKey);
            }
        }

        SubjectId? subject = null;
        if (request.SubjectId is not null)
        {
            if (!SubjectId.TryParse(request.SubjectId, out SubjectId parsed))
            {
                errors.Add(new ValidationErrorDto(
                    "/subject_id",
                    "pattern",
                    "subject_id must be an anon_ prefixed ULID."));
            }
            else
            {
                subject = parsed;
            }
        }

        DecisionContext context = ReadValueMap(
            request.Context,
            "/context",
            MaxContextKeys,
            errors,
            out Dictionary<string, string?> contextValues)
            ? DecisionContext.Create(contextValues)
            : DecisionContext.Empty;

        if (errors.Count > 0)
        {
            return false;
        }

        input = new DecisionInput(placement, eligible, context, subject);
        return true;
    }

    public static bool TryValidate(
        EventRequestDto? request,
        out EventInput input,
        out List<ValidationErrorDto> errors)
    {
        errors = [];
        input = null!;

        if (request is null)
        {
            errors.Add(new ValidationErrorDto("", "required", "A JSON body is required."));
            return false;
        }

        if (!EventId.TryParse(request.EventId, out EventId eventId))
        {
            errors.Add(new ValidationErrorDto(
                "/event_id",
                request.EventId is null ? "required" : "pattern",
                "event_id must be an evt_ prefixed ULID generated by the client."));
        }

        if (!EventType.TryParse(request.Type, out EventType type))
        {
            errors.Add(new ValidationErrorDto(
                "/type",
                request.Type is null ? "required" : "pattern",
                "type must match ^[a-z][a-z0-9_.-]{0,63}$."));
        }

        if (!TryParseUtcTimestamp(request.OccurredAt, out DateTimeOffset occurredAt))
        {
            errors.Add(new ValidationErrorDto(
                "/occurred_at",
                request.OccurredAt is null ? "required" : "format",
                "occurred_at must be an RFC 3339 timestamp with an explicit Z UTC offset."));
        }

        DecisionId? decision = null;
        if (request.DecisionId is not null)
        {
            if (!DecisionId.TryParse(request.DecisionId, out DecisionId parsed))
            {
                errors.Add(new ValidationErrorDto(
                    "/decision_id",
                    "pattern",
                    "decision_id must be a dec_ prefixed ULID."));
            }
            else
            {
                decision = parsed;
            }
        }

        SubjectId? subject = null;
        if (request.SubjectId is not null)
        {
            if (!SubjectId.TryParse(request.SubjectId, out SubjectId parsed))
            {
                errors.Add(new ValidationErrorDto(
                    "/subject_id",
                    "pattern",
                    "subject_id must be an anon_ prefixed ULID."));
            }
            else
            {
                subject = parsed;
            }
        }

        _ = ReadValueMap(
            request.Properties,
            "/properties",
            MaxPropertyKeys,
            errors,
            out Dictionary<string, string?> properties);

        if (errors.Count > 0)
        {
            return false;
        }

        input = new EventInput(eventId, type, occurredAt, decision, subject, properties);
        return true;
    }

    /// <summary>
    /// Only <c>Z</c> is accepted. A local offset would leave a stored timestamp
    /// ambiguous about which clock produced it, and silently converting one
    /// hides a misconfigured client.
    /// </summary>
    internal static bool TryParseUtcTimestamp(string? value, out DateTimeOffset timestamp)
    {
        timestamp = default;

        if (string.IsNullOrEmpty(value) || !value.EndsWith('Z'))
        {
            return false;
        }

        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind | DateTimeStyles.AdjustToUniversal,
            out timestamp);
    }

    private static bool ReadValueMap(
        IReadOnlyDictionary<string, JsonElement>? source,
        string pointerPrefix,
        int maxKeys,
        List<ValidationErrorDto> errors,
        out Dictionary<string, string?> values)
    {
        values = new Dictionary<string, string?>(StringComparer.Ordinal);

        if (source is null || source.Count == 0)
        {
            return true;
        }

        if (source.Count > maxKeys)
        {
            errors.Add(new ValidationErrorDto(
                pointerPrefix,
                "max_properties",
                $"At most {maxKeys.ToString(CultureInfo.InvariantCulture)} keys are accepted."));
            return false;
        }

        bool valid = true;
        foreach ((string key, JsonElement element) in source)
        {
            string pointer = $"{pointerPrefix}/{key}";

            if (!DecisionContext.IsValidKey(key))
            {
                errors.Add(new ValidationErrorDto(
                    pointer,
                    "unknown_context_key",
                    "Keys must match ^[a-z][a-z0-9_]{0,39}$ and be declared in tenant configuration."));
                valid = false;
                continue;
            }

            if (!TryReadScalar(element, out string? value))
            {
                errors.Add(new ValidationErrorDto(
                    pointer,
                    "unsupported_type",
                    "Values must be a string, number, boolean or null. Nested objects are not accepted."));
                valid = false;
                continue;
            }

            if (value is { Length: > MaxValueLength })
            {
                errors.Add(new ValidationErrorDto(
                    pointer,
                    "max_length",
                    $"Values are limited to {MaxValueLength.ToString(CultureInfo.InvariantCulture)} characters."));
                valid = false;
                continue;
            }

            values[key] = value;
        }

        return valid;
    }

    private static bool TryReadScalar(JsonElement element, out string? value)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                value = element.GetString();
                return true;
            case JsonValueKind.Number:
                value = element.GetRawText();
                return true;
            case JsonValueKind.True:
                value = "true";
                return true;
            case JsonValueKind.False:
                value = "false";
                return true;
            case JsonValueKind.Null:
                value = null;
                return true;
            default:
                value = null;
                return false;
        }
    }
}
