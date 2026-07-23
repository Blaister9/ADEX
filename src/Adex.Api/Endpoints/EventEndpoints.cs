using System.Globalization;
using Adex.Api.Contracts;
using Adex.Api.Http;
using Adex.Api.Telemetry;
using Adex.Api.Validation;
using Adex.Application.Events;
using Adex.Domain.Identifiers;

namespace Adex.Api.Endpoints;

public static class EventEndpoints
{
    public static RouteGroupBuilder MapEventEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/events", RecordEventAsync)
            .WithName("recordEvent")
            .WithSummary("Ingest one behavioural event, idempotently.");

        return group;
    }

    private static async Task<IResult> RecordEventAsync(
        EventRequestDto? request,
        HttpContext http,
        RecordEventHandler handler,
        CancellationToken cancellationToken)
    {
        TenantId tenant = TenantEndpointFilter.Current(http);

        if (!RequestValidation.TryValidate(request, out RequestValidation.EventInput input, out var errors))
        {
            return Problems.Validation(http, errors);
        }

        RecordEventResult result = await handler
            .HandleAsync(
                new RecordEventCommand(
                    tenant,
                    input.EventId,
                    input.Type,
                    input.OccurredAt,
                    input.Decision,
                    input.Subject,
                    input.Properties,
                    CorrelationIdMiddleware.Current(http)),
                cancellationToken)
            .ConfigureAwait(false);

        string outcome = result.Status switch
        {
            RecordEventStatus.Accepted => "accepted",
            RecordEventStatus.Duplicate => "duplicate",
            _ => "rejected",
        };

        AdexTelemetry.Events.Add(
            1,
            new KeyValuePair<string, object?>("tenant", tenant.Value),
            new KeyValuePair<string, object?>("type", input.Type.Value),
            new KeyValuePair<string, object?>("result", outcome));

        switch (result.Status)
        {
            case RecordEventStatus.RejectedClockSkew:
                return Problems.Validation(http, [
                    new ValidationErrorDto(
                        "/occurred_at",
                        "clock_skew",
                        $"occurred_at must be within {RecordEventHandler.MaxFutureSkew.TotalMinutes.ToString(CultureInfo.InvariantCulture)} "
                        + $"minutes ahead of and {RecordEventHandler.MaxPastSkew.TotalDays.ToString(CultureInfo.InvariantCulture)} days behind the server clock."),
                ]);

            case RecordEventStatus.UnknownDecision:
                // Scoped to this tenant, so this never reveals that another
                // tenant holds the decision.
                return Problems.Validation(http, [
                    new ValidationErrorDto(
                        "/decision_id",
                        "unknown_decision",
                        "No decision with this identifier exists for this tenant."),
                ]);

            default:
                // A replay is not an error: same status code, same shape, no
                // state change, so a client needs no special-case retry logic.
                return Results.Accepted(
                    value: new EventResponseDto(
                        input.EventId.Value,
                        outcome,
                        result.ReceivedAt.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture)));
        }
    }
}
