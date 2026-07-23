using System.Diagnostics;
using System.Globalization;
using Adex.Api.Contracts;
using Adex.Api.Http;
using Adex.Api.Telemetry;
using Adex.Api.Validation;
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
        CancellationToken cancellationToken)
    {
        TenantId tenant = TenantEndpointFilter.Current(http);

        if (!RequestValidation.TryValidate(request, out RequestValidation.DecisionInput input, out var errors))
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
                    CorrelationIdMiddleware.Current(http)),
                cancellationToken)
            .ConfigureAwait(false);

        double elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

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
}
