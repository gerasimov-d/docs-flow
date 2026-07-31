using System.Data.Common;
using Dapper;
using DocsFlow.Database;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace DocsFlow.Users.Tests;

public sealed class UsersMigrationTests(PostgresFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Users_table_is_created()
    {
        var factory = fixture.Services.GetRequiredService<IDbConnectionFactory>();
        await using var connection = await factory.OpenConnectionAsync(Ct);

        var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT to_regclass('users') IS NOT NULL",
            cancellationToken: Ct));

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task Keycloak_subject_is_unique()
    {
        var factory = fixture.Services.GetRequiredService<IDbConnectionFactory>();
        await using var connection = await factory.OpenConnectionAsync(Ct);

        var subject = Guid.CreateVersion7().ToString();

        await InsertAsync(connection, subject);

        // Связь с IdP обязана быть однозначной: два профиля на один sub означали бы, что
        // при входе непонятно, кого из них считать пользователем.
        await Should.ThrowAsync<DbException>(() => InsertAsync(connection, subject));
    }

    [Fact]
    public async Task Email_is_indexed_but_not_unique()
    {
        var factory = fixture.Services.GetRequiredService<IDbConnectionFactory>();
        await using var connection = await factory.OpenConnectionAsync(Ct);

        var definition = await connection.ExecuteScalarAsync<string>(new CommandDefinition(
            "SELECT indexdef FROM pg_indexes WHERE tablename = 'users' AND indexname = 'ix_users_email'",
            cancellationToken: Ct));

        definition.ShouldNotBeNull();

        // Уникальность email обеспечивает Keycloak (duplicateEmailsAllowed: false). Ограничение
        // здесь превратило бы расхождение настроек realm в 500 на входе пользователя.
        definition.ShouldNotContain("UNIQUE");
    }

    private static Task InsertAsync(DbConnection connection, string subject) =>
        connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO users (id, keycloak_subject, email) VALUES (@id, @subject, 'dup@docsflow.local')",
            new { id = Guid.CreateVersion7(), subject },
            cancellationToken: Ct));
}
