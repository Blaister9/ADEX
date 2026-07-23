using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Adex.Api.Contracts;
using Adex.Api.Http;
using Adex.Api.Telemetry;
using Adex.Api.Validation;
using Adex.Application.Abstractions;
using Adex.Application.Decisions;
using Adex.Domain.Decisions;
using Adex.Domain.Identifiers;

namespace Adex.Api.Endpoints;

public static class DecisionEndpoints
{
    public static RouteGroupBuilder MapDecisionEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/decisions", RequestDecisionAsync)
            .WithName("requestDecision")
            .WithSummary("Select one eligible alternative for a placement.");

        return group;
    }

    private static async Task<IResult> RequestDecisionAsync(
        DecisionRequestDto? request,
        HttpContext http,
        RequestDecisionHandler handler,
        IContextKeyPolicy contextKeyPolicy,
        CancellationToken cancellationToken)
    {
        TenantId tenant = TenantEndpointFilter.Current(http);
        string? idempotencyKey = ReadIdempotencyKey(http);
        if (http.Request.Headers.ContainsKey("Idempotency-Key") && idempotencyKey is null)
        {
            return Problems.Malformed(
                http,
                "Idempotency-Key must contain 8 to 64 letters, digits, dots, underscores or hyphens.");
        }

        IReadOnlySet<string> allowedContextKeys = await contextKeyPolicy
            .GetAllowedKeysAsync(tenant, cancellationToken)
            .ConfigureAwait(false);

        if (!RequestValidation.TryValidate(
                request,
                allowedContextKeys,
                out RequestValidation.DecisionInput input,
                out var errors))
        {
            return Problems.Validation(http, errors);
        }

        long startedAt = Stopwatch.GetTimestamp();

        using Activity? activity = AdexTelemetry.ActivitySource.StartActivity("adex.decision", ActivityKind.Internal);
        activity?.SetTag("adex.tenant_id", tenant.Value);
        activity?.SetTag("adex.placement", input.Placement.Value);

        RequestDecisionResult result = await handler
            .HandleAsync(
                new RequestDecisionCommand(
                    tenant,
                    input.Placement,
                    input.Eligible,
                    input.Context,
                    input.Subject,
                    CorrelationIdMiddleware.Current(http),
                    idempotencyKey,
                    idempotencyKey is null ? null : Fingerprint(request!)),
                cancellationToken)
            .ConfigureAwait(false);

        double elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        if (result.Status == RequestDecisionStatus.IdempotencyConflict)
        {
            return Problems.Conflict(
                http,
                Problems.IdempotencyKeyReused,
                "The idempotency key was already used.",
                "Reuse an Idempotency-Key only with the same decision request.");
        }

        if (result.Status == RequestDecisionStatus.NoEligibleAlternatives || result.Decision is null)
        {
            // ADEX never substitutes an alternative the caller did not declare.
            AdexTelemetry.Decisions.Add(
                1,
                new KeyValuePair<string, object?>("tenant", tenant.Value),
                new KeyValuePair<string, object?>("placement", input.Placement.Value),
                new KeyValuePair<string, object?>("outcome", "no_eligible_alternatives"));

            return Problems.Conflict(
                http,
                Problems.NoEligibleAlternatives,
                "No alternative remained eligible after constraints were applied.",
                "All declared alternatives were excluded. ADEX never substitutes an alternative "
                + "that the caller did not declare.");
        }

        DecisionRecord decision = result.Decision;

        activity?.SetTag("adex.decision_id", decision.Id.Value);
        activity?.SetTag("adex.policy_key", decision.Policy.Key);
        activity?.SetTag("adex.policy_version", decision.Policy.Version);

        AdexTelemetry.Decisions.Add(
            1,
            new KeyValuePair<string, object?>("tenant", tenant.Value),
            new KeyValuePair<string, object?>("placement", input.Placement.Value),
            new KeyValuePair<string, object?>("policy", decision.Policy.Key),
            new KeyValuePair<string, object?>("outcome", "decided"));

        AdexTelemetry.DecisionDuration.Record(
            elapsedMs,
            new KeyValuePair<string, object?>("tenant", tenant.Value),
            new KeyValuePair<string, object?>("placement", input.Placement.Value));

        return Results.Ok(new DecisionResponseDto(
            decision.Id.Value,
            decision.Selected.Value,
            new PolicyReferenceDto(decision.Policy.Key, decision.Policy.Version),
            decision.DecidedAt.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture)));
    }

    private static string? ReadIdempotencyKey(HttpContext http)
    {
        string value = http.Request.Headers["Idempotency-Key"].ToString();
        if (value.Length is < 8 or > 64)
        {
            return null;
        }

        return value.All(character =>
            character is >= 'A' and <= 'Z'
            || character is >= 'a' and <= 'z'
            || character is >= '0' and <= '9'
            || character is '.' or '_' or '-')
            ? value
            : null;
    }

    private static string Fingerprint(DecisionRequestDto request)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("placement", request.Placement);
            writer.WritePropertyName("eligible_alternatives");
            writer.WriteStartArray();
            foreach (EligibleAlternativeDto alternative in request.EligibleAlternatives!)
            {
                writer.WriteStartObject();
                writer.WriteString("key", alternative.Key);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            if (request.SubjectId is not null)
            {
                writer.WriteString("subject_id", request.SubjectId);
            }

            if (request.Context is not null)
            {
                writer.WritePropertyName("context");
                writer.WriteStartObject();
                foreach ((string key, JsonElement value) in request.Context.OrderBy(
                             pair => pair.Key,
                             StringComparer.Ordinal))
                {
                    writer.WritePropertyName(key);
                    value.WriteTo(writer);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        return Convert.ToHexString(SHA256.HashData(buffer.ToArray()));
    }
}
