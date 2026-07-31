using Shouldly;
using Xunit;

namespace DocsFlow.Spaces.Tests;

public sealed class ContextRepositoryTests(PostgresFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_context_is_created_inside_its_space()
    {
        var spaceId = await CreateSpaceAsync();

        var repository = fixture.Contexts(out var scope);
        using (scope)
        {
            var context = await repository.CreateAsync(spaceId, "Авто", Ct);

            context.ShouldNotBeNull();
            context.SpaceId.ShouldBe(spaceId);
            context.Name.ShouldBe("Авто");
        }
    }

    [Fact]
    public async Task A_space_without_contexts_is_a_normal_state()
    {
        var spaceId = await CreateSpaceAsync();

        var repository = fixture.Contexts(out var scope);
        using (scope)
        {
            // Контексты необязательны: пустой список — не ошибка и не повод что-то создавать.
            (await repository.ListAsync(spaceId, Ct)).ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task A_name_is_unique_within_a_space()
    {
        var spaceId = await CreateSpaceAsync();

        var repository = fixture.Contexts(out var scope);
        using (scope)
        {
            (await repository.CreateAsync(spaceId, "Ремонт", Ct)).ShouldNotBeNull();

            (await repository.CreateAsync(spaceId, "Ремонт", Ct)).ShouldBeNull();
            (await repository.ListAsync(spaceId, Ct)).Count.ShouldBe(1);
        }
    }

    [Fact]
    public async Task Names_differing_only_in_case_are_the_same_name()
    {
        var spaceId = await CreateSpaceAsync();

        var repository = fixture.Contexts(out var scope);
        using (scope)
        {
            await repository.CreateAsync(spaceId, "Авто", Ct);

            // «Авто» и «авто» в одном списке неразличимы на глаз — это ошибка ввода, а не второе
            // направление.
            (await repository.CreateAsync(spaceId, "авто", Ct)).ShouldBeNull();
        }
    }

    [Fact]
    public async Task The_same_name_lives_independently_in_different_spaces()
    {
        var first = await CreateSpaceAsync();
        var second = await CreateSpaceAsync();

        var repository = fixture.Contexts(out var scope);
        using (scope)
        {
            (await repository.CreateAsync(first, "Финансы", Ct)).ShouldNotBeNull();
            (await repository.CreateAsync(second, "Финансы", Ct)).ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task The_list_shows_only_the_contexts_of_its_own_space()
    {
        var mine = await CreateSpaceAsync();
        var foreign = await CreateSpaceAsync();

        var repository = fixture.Contexts(out var scope);
        using (scope)
        {
            await repository.CreateAsync(mine, "Своё", Ct);
            await repository.CreateAsync(foreign, "Чужое", Ct);

            var contexts = await repository.ListAsync(mine, Ct);

            contexts.ShouldHaveSingleItem().Name.ShouldBe("Своё");
        }
    }

    private async Task<Guid> CreateSpaceAsync()
    {
        var ownerId = await fixture.CreateUserAsync(Ct);

        var repository = fixture.Spaces(out var scope);
        using (scope)
        {
            var space = await repository.CreateAsync(ownerId, "Space для контекстов", Ct);

            return space.Id;
        }
    }
}
