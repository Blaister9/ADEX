using Adex.Api.Endpoints;
using Adex.Api.Http;
using Adex.Api.Telemetry;
using Adex.Application.Abstractions;
using Adex.Infrastructure.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

bool isDevelopment = builder.Environment.IsDevelopment();

// Payload caps are a boundary control, not a nicety: an unbounded body is a
// denial-of-service vector on a public, browser-facing endpoint (threats T6/T7).
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    // ASP.NET adds a camelCase `traceId`. ADEX already returns `correlation_id`,
    // which is the identifier a tenant quotes in a support request; two
    // identifiers under two naming conventions is worse than one.
    context.ProblemDetails.Extensions.Remove("traceId");
});
builder.Services.AddExceptionHandler<AdexExceptionHandler>();
builder.Services.AddSingleton<ICacheDegradationSink, OpenTelemetryCacheDegradationSink>();
builder.Services.AddAdexTelemetry(builder.Configuration);
builder.Services.AddAdexInfrastructure(builder.Configuration, isDevelopment);

WebApplication app = builder.Build();

// Fail at startup rather than on the first request: a missing seed salt or a
// malformed tenant map must not become a runtime 500.
app.Services.ValidateAdexConfiguration(isDevelopment);

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthEndpoints();

// Everything under /v1 is tenant-scoped. Adding a public endpoint therefore has
// to be a deliberate act outside this group (ADR-0007).
RouteGroupBuilder v1 = app.MapGroup("/v1").AddEndpointFilter<TenantEndpointFilter>();
v1.MapDecisionEndpoints();
v1.MapEventEndpoints();

app.Run();

/// <summary>
/// Exposed so the contract tests can host the real application through
/// <c>WebApplicationFactory</c> instead of testing a parallel wiring that could
/// drift from production.
/// </summary>
public partial class Program;
