using Dapper;
using DocsFlow.Database;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace DocsFlow.Users.Tests;

public sealed class UserRepositoryTests(PostgresFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_first_login_creates_the_user()
    {
        var subject = NewSubject();

        var user = await UpsertAsync(new ExternalIdentity(subject, "anna@docsflow.local", "Анна", true));

        user.Id.ShouldNotBe(Guid.Empty);
        user.KeycloakSubject.ShouldBe(subject);
        user.Email.ShouldBe("anna@docsflow.local");
        user.DisplayName.ShouldBe("Анна");
        user.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-5));
        user.LastLoginAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task A_repeated_login_updates_the_profile_without_creating_a_second_row()
    {
        var subject = NewSubject();

        var created = await UpsertAsync(new ExternalIdentity(subject, "old@docsflow.local", "Старое имя", true));
        var updated = await UpsertAsync(new ExternalIdentity(subject, "new@docsflow.local", "Новое имя", true));

        // Идентификатор пережил смену email — на него уже могли сослаться другие таблицы.
        updated.Id.ShouldBe(created.Id);
        updated.CreatedAt.ShouldBe(created.CreatedAt);
        updated.Email.ShouldBe("new@docsflow.local");
        updated.DisplayName.ShouldBe("Новое имя");
        updated.LastLoginAt!.Value.ShouldBeGreaterThanOrEqualTo(created.LastLoginAt!.Value);

        (await CountBySubjectAsync(subject)).ShouldBe(1);
    }

    [Fact]
    public async Task Concurrent_first_logins_do_not_race()
    {
        var subject = NewSubject();
        var identity = new ExternalIdentity(subject, "race@docsflow.local", "Гонка", true);

        // Ровно та ситуация, из-за которой в репозитории ON CONFLICT, а не SELECT + INSERT:
        // пользователь нажал «войти» в двух вкладках, и первый вход выполняется дважды.
        var results = await Task.WhenAll(
            UpsertAsync(identity),
            UpsertAsync(identity));

        results.Select(user => user.Id).Distinct().Count().ShouldBe(1);
        (await CountBySubjectAsync(subject)).ShouldBe(1);
    }

    [Fact]
    public async Task Get_returns_null_for_an_unknown_id()
    {
        using var scope = fixture.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        (await users.GetByIdAsync(Guid.CreateVersion7(), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task Get_returns_the_created_user()
    {
        var created = await UpsertAsync(new ExternalIdentity(NewSubject(), "get@docsflow.local", null, true));

        using var scope = fixture.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var found = await users.GetByIdAsync(created.Id, Ct);

        found.ShouldNotBeNull();
        found.KeycloakSubject.ShouldBe(created.KeycloakSubject);
        found.Email.ShouldBe("get@docsflow.local");
        found.DisplayName.ShouldBeNull();
    }

    // Каждый тест работает со своим sub: контейнер и база у класса общие.
    private static string NewSubject() => Guid.CreateVersion7().ToString();

    private async Task<User> UpsertAsync(ExternalIdentity identity)
    {
        // Отдельный scope на вызов: репозиторий scoped, и параллельным вызовам нужны свои соединения.
        using var scope = fixture.Services.CreateScope();

        return await scope.ServiceProvider
            .GetRequiredService<IUserRepository>()
            .UpsertBySubjectAsync(identity, Ct);
    }

    private async Task<long> CountBySubjectAsync(string subject)
    {
        var factory = fixture.Services.GetRequiredService<IDbConnectionFactory>();
        await using var connection = await factory.OpenConnectionAsync(Ct);

        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT count(*) FROM users WHERE keycloak_subject = @subject",
            new { subject },
            cancellationToken: Ct));
    }
}
