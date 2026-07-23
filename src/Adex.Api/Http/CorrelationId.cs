using System.Diagnostics;
using Adex.Infrastructure.Identifiers;

namespace Adex.Api.Http;

/// <summary>
/// Establishes the identifier a tenant quotes in a support request.
///
/// A client-supplied value is accepted only if it fits the contract's grammar —
/// it ends up in logs and traces, so an unbounded or control-character-laden
/// value would be a log-injection vector. Otherwise one is generated. The value
/// is echoed on every response, including errors.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "adex.correlation_id";

    private const int MinLength = 8;
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string correlationId = Sanitize(context.Request.Headers[HeaderName].ToString())
            ?? UlidIdentifierGenerator.NewUlid();

        context.Items[ItemKey] = correlationId;
        Activity.Current?.SetTag("adex.correlation_id", correlationId);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        await next(context).ConfigureAwait(false);
    }

    internal static string? Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length is < MinLength or > MaxLength)
        {
            return null;
        }

        foreach (char character in value)
        {
            bool allowed =
                character is >= 'A' and <= 'Z'
                || character is >= 'a' and <= 'z'
                || character is >= '0' and <= '9'
                || character is '.' or '_' or '-';
            if (!allowed)
            {
                return null;
            }
        }

        return value;
    }

    public static string Current(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(ItemKey, out object? value) && value is string correlationId
            ? correlationId
            : string.Empty;
    }
}
