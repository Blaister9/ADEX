using Adex.Application.Abstractions;
using Adex.Domain.Identifiers;

namespace Adex.Api.Http;

/// <summary>
/// Resolves the tenant once per request from the API key header and puts it in
/// <see cref="HttpContext.Items"/>.
///
/// It is a per-endpoint filter rather than global middleware so that adding a
/// public endpoint is a deliberate act: an endpoint either sits inside the
/// tenant-scoped group or it does not. There is no ambient "current tenant"
/// that a background task could inherit (ADR-0007).
/// </summary>
public sealed class TenantEndpointFilter(ITenantDirectory tenants) : IEndpointFilter
{
    public const string HeaderName = "X-Adex-Api-Key";
    public const string ItemKey = "adex.tenant";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        HttpContext http = context.HttpContext;
        string apiKey = http.Request.Headers[HeaderName].ToString();

        TenantId? tenant = string.IsNullOrEmpty(apiKey)
            ? null
            : await tenants.ResolveByApiKeyAsync(apiKey, http.RequestAborted).ConfigureAwait(false);

        if (tenant is null)
        {
            return Problems.Unauthorized(http);
        }

        http.Items[ItemKey] = tenant.Value;
        return await next(context).ConfigureAwait(false);
    }

    /// <summary>
    /// The tenant for the current request. Throws when called outside the
    /// tenant-scoped endpoint group, which is a programming error rather than a
    /// runtime condition.
    /// </summary>
    public static TenantId Current(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Items.TryGetValue(ItemKey, out object? value) && value is TenantId tenant)
        {
            return tenant;
        }

        throw new InvalidOperationException(
            "No tenant is bound to this request. The endpoint is missing the tenant filter.");
    }
}
