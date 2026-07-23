using Adex.Application.Abstractions;
using Adex.Application.Decisions;
using Adex.Application.Events;
using Adex.Application.Health;
using Adex.Infrastructure.Caching;
using Adex.Infrastructure.Identifiers;
using Adex.Infrastructure.Persistence.InMemory;
using Adex.Infrastructure.Persistence.Postgres;
using Adex.Infrastructure.Policies;
using Adex.Infrastructure.Privacy;
using Adex.Infrastructure.Tenancy;
using Adex.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adex.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers the adapters behind the application ports.
    /// </summary>
    /// <param name="isDevelopment">
    /// Development-only adapters (configuration-based tenants, in-memory stores)
    /// refuse to register outside development, so a misconfigured deployment
    /// fails at startup instead of quietly losing every decision.
    /// </param>
    public static IServiceCollection AddAdexInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));
        services.Configure<PrivacyOptions>(configuration.GetSection(PrivacyOptions.SectionName));
        services.Configure<TenancyOptions>(configuration.GetSection(TenancyOptions.SectionName));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdentifierGenerator, UlidIdentifierGenerator>();
        services.AddSingleton<ISeedSaltProvider, ConfiguredSeedSaltProvider>();
        services.AddSingleton<IPolicyResolver, UniformRandomPolicyResolver>();
        services.AddSingleton<ITenantDirectory, ConfiguredTenantDirectory>();

        PersistenceProvider provider = ReadProvider(configuration);
        AddPersistence(services, provider, isDevelopment);
        AddProbes(services, configuration, provider);

        services.AddScoped<RequestDecisionHandler>();
        services.AddScoped<RecordEventHandler>();

        return services;
    }

    private static PersistenceProvider ReadProvider(IConfiguration configuration) =>
        configuration
            .GetSection(PersistenceOptions.SectionName)
            .Get<PersistenceOptions>()?.Provider ?? PersistenceProvider.InMemory;

    private static void AddPersistence(
        IServiceCollection services,
        PersistenceProvider provider,
        bool isDevelopment)
    {
        switch (provider)
        {
            case PersistenceProvider.InMemory:
                if (!isDevelopment)
                {
                    throw new InvalidOperationException(
                        "The in-memory persistence provider is development-only and loses every "
                        + "decision on restart. Set Adex:Persistence:Provider=Postgres once the "
                        + "PostgreSQL adapters exist (docs/planning/foundation-roadmap.md, task 004).");
                }

                services.AddSingleton<InMemoryDecisionStore>();
                services.AddSingleton<InMemoryEventStore>();
                services.AddSingleton<IDecisionStore>(sp => sp.GetRequiredService<InMemoryDecisionStore>());
                services.AddSingleton<IEventStore>(sp => sp.GetRequiredService<InMemoryEventStore>());
                break;

            case PersistenceProvider.Postgres:
                // Deliberately not silently falling back to the in-memory store:
                // that would look like a working system of record and behave like
                // a cache. The adapters are roadmap task 004.
                throw new NotSupportedException(
                    "Adex:Persistence:Provider=Postgres is not implemented yet. The PostgreSQL "
                    + "schema and adapters are roadmap task 004 "
                    + "(docs/planning/foundation-roadmap.md). Use InMemory for local development.");

            default:
                throw new InvalidOperationException($"Unknown persistence provider '{provider}'.");
        }
    }

    private static void AddProbes(
        IServiceCollection services,
        IConfiguration configuration,
        PersistenceProvider provider)
    {
        string? postgres = configuration.GetConnectionString("Postgres");
        string? redis = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(postgres))
        {
            // Required only when PostgreSQL actually backs the stores. Under the
            // development in-memory provider its outage degrades readiness
            // instead of failing it, because the API keeps working.
            bool required = provider == PersistenceProvider.Postgres;
            services.AddSingleton<IDependencyProbe>(sp => new PostgresDependencyProbe(
                postgres,
                required,
                sp.GetRequiredService<ILogger<PostgresDependencyProbe>>()));
        }

        services.AddSingleton<IDependencyProbe>(sp => new RedisDependencyProbe(
            redis,
            sp.GetRequiredService<ILogger<RedisDependencyProbe>>()));
    }

    /// <summary>
    /// Validates configuration that must be right before the process accepts
    /// traffic. Called from the API composition root.
    /// </summary>
    public static void ValidateAdexConfiguration(this IServiceProvider services, bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Constructing these eagerly turns a bad salt or a malformed tenant map
        // into a startup failure rather than a runtime 500 on the first request.
        _ = services.GetRequiredService<ISeedSaltProvider>().GetSalt();

        var tenancy = services.GetRequiredService<IOptions<TenancyOptions>>();
        if (!isDevelopment && tenancy.Value.DevelopmentApiKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"{TenancyOptions.SectionName}:DevelopmentApiKeys is configured outside the "
                + "Development environment. Development keys are unhashed, unrotatable and "
                + "origin-unrestricted; they must never be used in a shared environment.");
        }

        _ = services.GetRequiredService<ITenantDirectory>();
    }
}
