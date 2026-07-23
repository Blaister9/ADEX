namespace Adex.Application.Abstractions;

/// <summary>
/// Supplies the environment-scoped salt used to derive decision seeds. Rotating
/// it deliberately reshuffles every assignment, so it is an operator action with
/// a documented consequence (see <see cref="Domain.Policies.DecisionSeed"/>).
/// </summary>
public interface ISeedSaltProvider
{
    byte[] GetSalt();
}
