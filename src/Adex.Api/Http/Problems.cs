using Adex.Api.Contracts;

namespace Adex.Api.Http;

/// <summary>
/// RFC 9457 problem responses with the two ADEX extensions: <c>correlation_id</c>
/// and <c>errors</c>.
///
/// Clients branch on <c>type</c>, never on the human-readable title, which is
/// why the types are constants here and in
/// <c>packages/contracts/src/index.ts</c>. Details never contain internal
/// identifiers, SQL or another tenant's data.
/// </summary>
public static class Problems
{
    public const string TypeBase = "https://contracts.adex.dev/problems";

    public const string ValidationFailed = TypeBase + "/validation-failed";
    public const string NoEligibleAlternatives = TypeBase + "/no-eligible-alternatives";
    public const string IdempotencyKeyReused = TypeBase + "/idempotency-key-reused";
    public const string Unauthenticated = TypeBase + "/unauthenticated";
    public const string DependencyUnavailable = TypeBase + "/dependency-unavailable";
    public const string MalformedRequest = TypeBase + "/malformed-request";

    public static IResult Validation(HttpContext context, IReadOnlyList<ValidationErrorDto> errors) =>
        Results.Problem(
            detail: "One or more fields are invalid. See errors.",
            instance: context.Request.Path,
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "The request body failed validation.",
            type: ValidationFailed,
            extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["correlation_id"] = CorrelationIdMiddleware.Current(context),
                ["errors"] = errors,
            });

    public static IResult Malformed(HttpContext context, string detail) =>
        Results.Problem(
            detail: detail,
            instance: context.Request.Path,
            statusCode: StatusCodes.Status400BadRequest,
            title: "The request could not be read.",
            type: MalformedRequest,
            extensions: Correlation(context));

    public static IResult Unauthorized(HttpContext context) =>
        Results.Problem(
            detail: "A valid tenant API key is required in the X-Adex-Api-Key header.",
            instance: context.Request.Path,
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Missing or unknown API key.",
            type: Unauthenticated,
            extensions: Correlation(context));

    public static IResult Conflict(HttpContext context, string type, string title, string detail) =>
        Results.Problem(
            detail: detail,
            instance: context.Request.Path,
            statusCode: StatusCodes.Status409Conflict,
            title: title,
            type: type,
            extensions: Correlation(context));

    private static Dictionary<string, object?> Correlation(HttpContext context) =>
        new(StringComparer.Ordinal)
        {
            ["correlation_id"] = CorrelationIdMiddleware.Current(context),
        };
}
