using Npgsql;

namespace Adex.IntegrationTests;

/// <summary>
/// Asserts that the bootstrap migration in <c>infra/local/migrations</c> actually
/// ran inside the PostgreSQL container.
///
/// Deliberately narrow: the foundation ships only the schema and the migration
/// ledger. Entity tables, composite keys and Row Level Security are designed
/// with the first vertical slice (roadmap task 004), and asserting a schema that
/// no code has validated would be theatre.
/// </summary>
public sealed class BootstrapMigrationTests
{
    private static async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(InfrastructureAvailability.PostgresConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    [Fact]
    public async Task Creates_the_adex_schema()
    {
        InfrastructureAvailability.RequireInfrastructure();

        await using NpgsqlConnection connection = await OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "select count(*) from information_schema.schemata where schema_name = 'adex'";

        object? count = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1L, Assert.IsType<long>(count));
    }

    [Fact]
    public async Task Records_the_bootstrap_migration_in_the_ledger()
    {
        InfrastructureAvailability.RequireInfrastructure();

        await using NpgsqlConnection connection = await OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "select version from adex.schema_migrations order by version";

        var applied = new List<string>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            applied.Add(reader.GetString(0));
        }

        Assert.Contains("0001_bootstrap", applied);
    }

    [Fact]
    public async Task Stores_timestamps_in_utc()
    {
        InfrastructureAvailability.RequireInfrastructure();

        // ADEX stores no local times; a database defaulting to a local zone would
        // corrupt every attribution window.
        await using NpgsqlConnection connection = await OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "show timezone";

        object? timezone = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        Assert.Equal("UTC", Assert.IsType<string>(timezone), ignoreCase: true);
    }
}
