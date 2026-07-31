using Shouldly;
using Xunit;

namespace DocsFlow.Spaces.Tests;

public sealed class SpaceRepositoryTests(PostgresFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_creator_becomes_the_owner()
    {
        var userId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            var space = await repository.CreateAsync(userId, "Семейный архив", Ct);

            space.Id.ShouldNotBe(Guid.Empty);
            space.Name.ShouldBe("Семейный архив");

            (await repository.FindRoleAsync(space.Id, userId, Ct)).ShouldBe(SpaceRole.Owner);
        }
    }

    [Fact]
    public async Task A_user_may_own_any_number_of_spaces()
    {
        var userId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            await repository.CreateAsync(userId, "Первый", Ct);
            await repository.CreateAsync(userId, "Второй", Ct);
            await repository.CreateAsync(userId, "Третий", Ct);

            var memberships = await repository.ListForUserAsync(userId, Ct);

            memberships.Count.ShouldBe(3);
            memberships.ShouldAllBe(membership => membership.Role == SpaceRole.Owner);
        }
    }

    [Fact]
    public async Task The_list_shows_the_role_in_each_space()
    {
        var ownerId = await fixture.CreateUserAsync(Ct);
        var memberId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            var own = await repository.CreateAsync(memberId, "Своё", Ct);
            var shared = await repository.CreateAsync(ownerId, "Общее", Ct);

            await repository.AddMemberAsync(shared.Id, memberId, Ct);

            var memberships = await repository.ListForUserAsync(memberId, Ct);

            memberships.Count.ShouldBe(2);
            memberships.Single(membership => membership.Id == own.Id).Role.ShouldBe(SpaceRole.Owner);
            memberships.Single(membership => membership.Id == shared.Id).Role.ShouldBe(SpaceRole.Member);
        }
    }

    [Fact]
    public async Task A_stranger_has_no_role_in_someone_elses_space()
    {
        var ownerId = await fixture.CreateUserAsync(Ct);
        var strangerId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            var space = await repository.CreateAsync(ownerId, "Закрытое", Ct);

            // Ровно то же, что репозиторий отвечает про несуществующий space: снаружи эти два
            // случая обязаны быть неразличимы.
            (await repository.FindRoleAsync(space.Id, strangerId, Ct)).ShouldBeNull();
            (await repository.FindRoleAsync(Guid.CreateVersion7(), strangerId, Ct)).ShouldBeNull();
        }
    }

    [Fact]
    public async Task Access_is_granted_and_revoked()
    {
        var ownerId = await fixture.CreateUserAsync(Ct);
        var memberId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            var space = await repository.CreateAsync(ownerId, "Общее", Ct);

            (await repository.AddMemberAsync(space.Id, memberId, Ct)).ShouldBe(AddMemberResult.Added);
            (await repository.FindRoleAsync(space.Id, memberId, Ct)).ShouldBe(SpaceRole.Member);

            (await repository.RemoveMemberAsync(space.Id, memberId, Ct)).ShouldBe(RemoveMemberResult.Removed);
            (await repository.FindRoleAsync(space.Id, memberId, Ct)).ShouldBeNull();
        }
    }

    [Fact]
    public async Task Granting_access_twice_changes_nothing()
    {
        var ownerId = await fixture.CreateUserAsync(Ct);
        var memberId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            var space = await repository.CreateAsync(ownerId, "Общее", Ct);

            await repository.AddMemberAsync(space.Id, memberId, Ct);

            (await repository.AddMemberAsync(space.Id, memberId, Ct)).ShouldBe(AddMemberResult.AlreadyMember);
            (await repository.ListMembersAsync(space.Id, Ct)).Count.ShouldBe(2);
        }
    }

    [Fact]
    public async Task An_unknown_user_cannot_be_invited()
    {
        var ownerId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            var space = await repository.CreateAsync(ownerId, "Общее", Ct);

            // Приглашений незарегистрированных пока нет: такой идентификатор — просто промах.
            (await repository.AddMemberAsync(space.Id, Guid.CreateVersion7(), Ct))
                .ShouldBe(AddMemberResult.UserNotFound);
        }
    }

    [Fact]
    public async Task Revoking_access_of_a_non_member_is_not_an_error()
    {
        var ownerId = await fixture.CreateUserAsync(Ct);
        var strangerId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            var space = await repository.CreateAsync(ownerId, "Общее", Ct);

            (await repository.RemoveMemberAsync(space.Id, strangerId, Ct)).ShouldBe(RemoveMemberResult.NotMember);
        }
    }

    [Fact]
    public async Task The_owner_cannot_be_removed_from_their_own_space()
    {
        var ownerId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            var space = await repository.CreateAsync(ownerId, "Личное", Ct);

            // Иначе space остался бы без владельца, а передачи владения нет.
            (await repository.RemoveMemberAsync(space.Id, ownerId, Ct))
                .ShouldBe(RemoveMemberResult.OwnerCannotBeRemoved);

            (await repository.FindRoleAsync(space.Id, ownerId, Ct)).ShouldBe(SpaceRole.Owner);
        }
    }

    [Fact]
    public async Task A_space_can_be_renamed()
    {
        var ownerId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            var space = await repository.CreateAsync(ownerId, "Старое имя", Ct);

            (await repository.RenameAsync(space.Id, "Новое имя", Ct)).ShouldBeTrue();

            var found = await repository.FindAsync(space.Id, Ct);

            found.ShouldNotBeNull();
            found.Name.ShouldBe("Новое имя");
            found.UpdatedAt.ShouldBeGreaterThanOrEqualTo(space.UpdatedAt);
        }
    }

    [Fact]
    public async Task Renaming_a_missing_space_reports_failure()
    {
        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            (await repository.RenameAsync(Guid.CreateVersion7(), "Неважно", Ct)).ShouldBeFalse();
        }
    }

    [Fact]
    public async Task The_members_list_carries_profiles_and_roles()
    {
        var ownerId = await fixture.CreateUserAsync(Ct);
        var memberId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            var space = await repository.CreateAsync(ownerId, "Общее", Ct);
            await repository.AddMemberAsync(space.Id, memberId, Ct);

            var members = await repository.ListMembersAsync(space.Id, Ct);

            members.Count.ShouldBe(2);
            members.Single(member => member.UserId == ownerId).Role.ShouldBe(SpaceRole.Owner);

            var member = members.Single(candidate => candidate.UserId == memberId);
            member.Role.ShouldBe(SpaceRole.Member);
            member.Email.ShouldNotBeNullOrWhiteSpace();
            member.DisplayName.ShouldBe("Тестовый пользователь");
        }
    }

    [Fact]
    public async Task The_first_space_is_created_once()
    {
        var userId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            var created = await repository.CreateFirstIfMissingAsync(userId, "Личное", Ct);

            created.ShouldNotBeNull();
            created.Name.ShouldBe("Личное");

            // Второй вход ничего не создаёт: «хотя бы один space» уже выполнено.
            (await repository.CreateFirstIfMissingAsync(userId, "Личное", Ct)).ShouldBeNull();
            (await repository.ListForUserAsync(userId, Ct)).Count.ShouldBe(1);
        }
    }

    [Fact]
    public async Task A_user_who_already_has_a_space_gets_no_second_one()
    {
        var ownerId = await fixture.CreateUserAsync(Ct);
        var memberId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            var shared = await repository.CreateAsync(ownerId, "Общее", Ct);
            await repository.AddMemberAsync(shared.Id, memberId, Ct);

            // Участие в чужом space — тоже «есть куда грузить»: свой заводить не за чем.
            (await repository.CreateFirstIfMissingAsync(memberId, "Личное", Ct)).ShouldBeNull();
        }
    }

    [Fact]
    public async Task Concurrent_first_logins_create_a_single_space()
    {
        var userId = await fixture.CreateUserAsync(Ct);

        // Ровно та ситуация, из-за которой первый space создаётся под блокировкой строки
        // пользователя: вход выполняется дважды одновременно, например из двух вкладок.
        var results = await Task.WhenAll(
            CreateFirstAsync(userId),
            CreateFirstAsync(userId));

        results.Count(space => space is not null).ShouldBe(1);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            (await repository.ListForUserAsync(userId, Ct)).Count.ShouldBe(1);
        }
    }

    private async Task<Space?> CreateFirstAsync(Guid userId)
    {
        // Отдельный scope на вызов: репозиторий scoped, и параллельным вызовам нужны свои соединения.
        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            return await repository.CreateFirstIfMissingAsync(userId, "Личное", Ct);
        }
    }
}
