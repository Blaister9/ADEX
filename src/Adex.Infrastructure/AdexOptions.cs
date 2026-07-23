using System.ComponentModel.DataAnnotations;

namespace Adex.Infrastructure;

public enum PersistenceProvider
{
    /// <summary>Development-only, process-local storage. Not a system of record.</summary>
    InMemory,

    /// <summary>The intended production provider. Not implemented yet — see roadmap task 004.</summary>
    Postgres,
}

public sealed class PersistenceOptions
{
    public const string SectionName = "Adex:Persistence";

    public PersistenceProvider Provider { get; set; } = PersistenceProvider.InMemory;
}

public sealed class PrivacyOptions
{
    public const string SectionName = "Adex:Privacy";

    /// <summary>
    /// Environment-scoped, rotatable salt for decision seeds and for hashing
    /// tenant-supplied identifiers. Never committed; supplied through the
    /// environment (ADR-0008).
    /// </summary>
    [Required]
    public string SubjectSalt { get; set; } = string.Empty;
}

public sealed class TenancyOptions
{
    public const string SectionName = "Adex:Tenancy";

    /// <summary>
    /// API key to tenant identifier map read from configuration.
    ///
    /// This is a <em>development</em> tenant directory: it holds no key hashing,
    /// no origin allow-list, no rotation and no revocation. The real directory,
    /// backed by PostgreSQL, is roadmap task 004. The API refuses to start with
    /// configured keys outside the Development environment.
    /// </summary>
    public Dictionary<string, string> DevelopmentApiKeys { get; } = new(StringComparer.Ordinal);
}
