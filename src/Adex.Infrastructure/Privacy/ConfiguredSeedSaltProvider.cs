using System.Text;
using Adex.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Adex.Infrastructure.Privacy;

/// <summary>
/// Reads the decision-seed salt from configuration, which in practice means an
/// environment variable or a secret store — never a committed file.
///
/// The provider fails fast when the salt is missing or obviously a placeholder,
/// because a silently empty salt would make every tenant's assignment
/// predictable from public information.
/// </summary>
public sealed class ConfiguredSeedSaltProvider : ISeedSaltProvider
{
    private const int MinimumSaltLength = 16;

    private readonly byte[] _salt;

    public ConfiguredSeedSaltProvider(IOptions<PrivacyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string configured = options.Value.SubjectSalt;
        if (string.IsNullOrWhiteSpace(configured) || configured.Length < MinimumSaltLength)
        {
            throw new InvalidOperationException(
                $"{PrivacyOptions.SectionName}:SubjectSalt must be configured with at least "
                + $"{MinimumSaltLength} characters. See .env.example and ADR-0008.");
        }

        _salt = Encoding.UTF8.GetBytes(configured);
    }

    public byte[] GetSalt() => _salt;
}
