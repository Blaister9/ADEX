namespace Adex.Application.Abstractions;

/// <summary>Records that an optional cache dependency could not be used.</summary>
public interface ICacheDegradationSink
{
    void Record(string reason);
}
