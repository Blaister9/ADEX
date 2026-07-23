using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Adex.Domain.Identifiers;
using Adex.Domain.Placements;

namespace Adex.Domain.Policies;

/// <summary>
/// Derives the deterministic draw a policy uses.
///
/// The seed is a keyed hash of the tenant, placement, policy version and the
/// subject (falling back to the decision identifier when there is no subject).
/// Two consequences that the product depends on:
/// <list type="bullet">
///   <item>the same subject keeps the same assignment across page views, which
///   is what makes an A/B split sticky;</item>
///   <item>a stored decision can be recomputed offline and compared with what
///   was actually served — the replay check that gates every policy.</item>
/// </list>
/// Rotating <c>salt</c> deliberately reshuffles every assignment. That is an
/// operator action with a documented consequence, not a routine one.
/// </summary>
public static class DecisionSeed
{
    public static uint Derive(
        ReadOnlySpan<byte> salt,
        TenantId tenant,
        PlacementKey placement,
        PolicyReference policy,
        string subjectOrDecisionId)
    {
        ArgumentException.ThrowIfNullOrEmpty(subjectOrDecisionId);

        // A unit separator between fields, so two different field splits can
        // never produce identical material and therefore an identical assignment.
        string material = string.Join(
            '\u001f',
            tenant.Value,
            placement.Value,
            policy.Key,
            policy.Version.ToString(CultureInfo.InvariantCulture),
            subjectOrDecisionId);

        Span<byte> hash = stackalloc byte[HMACSHA256.HashSizeInBytes];
        HMACSHA256.HashData(salt, Encoding.UTF8.GetBytes(material), hash);

        // Little-endian explicitly, not BitConverter's architecture-dependent
        // order: the Python simulator has to derive the identical seed for the
        // cross-language equivalence fixtures to mean anything (ADR-0003).
        return BinaryPrimitives.ReadUInt32LittleEndian(hash);
    }
}
