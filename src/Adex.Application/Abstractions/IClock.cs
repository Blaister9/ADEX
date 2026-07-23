namespace Adex.Application.Abstractions;

/// <summary>
/// The only source of time in the application layer. Injected so that decision
/// and ingestion behaviour is testable without waiting, and so no domain code
/// ever reads the ambient clock (which would break replayability).
/// </summary>
public interface IClock
{
    /// <summary>Current instant, always in UTC.</summary>
    DateTimeOffset UtcNow { get; }
}
