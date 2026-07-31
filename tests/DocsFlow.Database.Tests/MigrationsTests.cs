using Dapper;
using DocsFlow.Database;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace DocsFlow.Database.Tests;

public sealed class MigrationsTests(PostgresFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Demo_notes_table_is_created()
        => (await TableExistsAsync("demo_notes")).ShouldBeTrue();

    [Fact]
    public async Task Version_table_is_snake_case_and_records_the_migration()
    {
        (await TableExistsAsync("version_info")).ShouldBeTrue();

        var factory = fixture.Services.GetRequiredService<IDbConnectionFactory>();
        await using var connection = await factory.OpenConnectionAsync(Ct);

        var applied = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT count(*) FROM version_info WHERE version = @version",
            new { version = 20260730120000L },
            cancellationToken: Ct));

        applied.ShouldBe(1);
    }

    private async Task<bool> TableExistsAsync(string table)
    {
        var factory = fixture.Services.GetRequiredService<IDbConnectionFactory>();
        await using var connection = await factory.OpenConnectionAsync(Ct);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT to_regclass(@table) IS NOT NULL",
            new { table },
            cancellationToken: Ct));
    }
}
