using Dapper;
using DocsFlow.Database;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace DocsFlow.Spaces.Tests;

/// <summary>
/// Проверяет ограничения схемы — те, что держат инварианты фичи независимо от кода приложения.
/// </summary>
public sealed class SpacesMigrationTests(PostgresFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_space_cannot_have_two_owners()
    {
        var ownerId = await fixture.CreateUserAsync(Ct);
        var otherId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            var space = await repository.CreateAsync(ownerId, "Общее", Ct);

            // Второй владелец не появится даже в обход репозитория: ограничение держит база.
            var error = await Should.ThrowAsync<Npgsql.PostgresException>(() => ExecuteAsync(
                "INSERT INTO space_members (space_id, user_id, role) VALUES (@spaceId, @otherId, 'owner')",
                new { spaceId = space.Id, otherId }));

            error.SqlState.ShouldBe("23505");
        }
    }

    [Fact]
    public async Task An_unknown_role_is_rejected()
    {
        var ownerId = await fixture.CreateUserAsync(Ct);
        var otherId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            var space = await repository.CreateAsync(ownerId, "Общее", Ct);

            // Ролей тоньше «владельца» и «участника» нет: опечатка не должна превращаться
            // в невидимую дыру в проверке прав.
            var error = await Should.ThrowAsync<Npgsql.PostgresException>(() => ExecuteAsync(
                "INSERT INTO space_members (space_id, user_id, role) VALUES (@spaceId, @otherId, 'admin')",
                new { spaceId = space.Id, otherId }));

            error.SqlState.ShouldBe("23514");
        }
    }

    [Fact]
    public async Task A_context_cannot_outlive_its_space()
    {
        var foreignKeys = await ScalarAsync<int>(
            """
            SELECT count(*)
            FROM pg_constraint
            WHERE conrelid = 'contexts'::regclass AND conname = 'fk_contexts_space' AND contype = 'f'
            """);

        foreignKeys.ShouldBe(1);
    }

    private async Task ExecuteAsync(string sql, object parameters)
    {
        var factory = fixture.Services.GetRequiredService<IDbConnectionFactory>();
        await using var connection = await factory.OpenConnectionAsync(Ct);

        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: Ct));
    }

    private async Task<T?> ScalarAsync<T>(string sql)
    {
        var factory = fixture.Services.GetRequiredService<IDbConnectionFactory>();
        await using var connection = await factory.OpenConnectionAsync(Ct);

        return await connection.ExecuteScalarAsync<T>(new CommandDefinition(sql, cancellationToken: Ct));
    }
}
