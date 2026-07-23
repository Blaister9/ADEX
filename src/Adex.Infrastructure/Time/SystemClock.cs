using Adex.Application.Abstractions;

namespace Adex.Infrastructure.Time;

/// <summary>The production clock. Always UTC; ADEX stores no local times.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
