using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Xunit;

namespace DocsFlow.Api.Tests;

/// <summary>
/// Сквозные проверки space и контекстов: настоящий вход, настоящие cookie, настоящая база.
/// Изоляция данных арендатора проверяется именно здесь — на уровне репозиториев видно только то,
/// что фильтр по space применён, но не то, что конвейер API вообще к нему обратился.
/// </summary>
public sealed class SpaceApiTests(DocsFlowAppFixture fixture)
{
    private sealed record Me(Guid Id, string Email, string? DisplayName);

    private sealed record SpaceResponse(Guid Id, string Name, string Role, DateTime CreatedAt);

    private sealed record MemberResponse(Guid UserId, string Email, string? DisplayName, string Role);

    private sealed record ContextResponse(Guid Id, string Name, DateTime CreatedAt);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Spaces_are_not_listed_without_a_session()
    {
        using var client = fixture.CreateClient();

        using var response = await client.GetAsync("/api/spaces", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_first_login_leaves_the_user_with_a_space()
    {
        using var client = fixture.CreateClient();
        await LogInAsync(client, DocsFlowAppFixture.FreshUserEmail, DocsFlowAppFixture.FreshUserPassword);

        var spaces = await ListSpacesAsync(client);

        // Требование фичи: состояние «залогинен, но грузить некуда» недопустимо.
        var space = spaces.ShouldHaveSingleItem();
        space.Role.ShouldBe("owner");
        space.Name.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task A_created_space_appears_in_the_list()
    {
        using var client = fixture.CreateClient();
        await LogInAsync(client);

        var created = await CreateSpaceAsync(client, "Документы по машине");

        created.Role.ShouldBe("owner");

        var spaces = await ListSpacesAsync(client);
        spaces.ShouldContain(space => space.Id == created.Id && space.Name == "Документы по машине");
    }

    [Fact]
    public async Task A_space_without_a_name_is_rejected()
    {
        using var client = fixture.CreateClient();
        await LogInAsync(client);

        using var response = await client.SendJsonAsync(HttpMethod.Post, "/api/spaces", new { name = "   " }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Someone_elses_space_is_indistinguishable_from_a_missing_one()
    {
        using var owner = fixture.CreateClient();
        await LogInAsync(owner);
        var foreign = await CreateSpaceAsync(owner, "Чужой архив");

        using var stranger = fixture.CreateClient();
        await LogInAsync(stranger, DocsFlowAppFixture.OtherUserEmail, DocsFlowAppFixture.OtherUserPassword);

        using var toForeign = await stranger.GetAsync($"/api/spaces/{foreign.Id}", Ct);
        using var toMissing = await stranger.GetAsync($"/api/spaces/{Guid.CreateVersion7()}", Ct);

        // Существование чужого space не раскрывается: ответы обязаны совпадать.
        toForeign.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        toMissing.StatusCode.ShouldBe(toForeign.StatusCode);
    }

    [Fact]
    public async Task Contexts_of_someone_elses_space_are_not_readable()
    {
        using var owner = fixture.CreateClient();
        await LogInAsync(owner);
        var foreign = await CreateSpaceAsync(owner, "Чужой архив с контекстами");
        await CreateContextAsync(owner, foreign.Id, "Ребёнок");

        using var stranger = fixture.CreateClient();
        await LogInAsync(stranger, DocsFlowAppFixture.OtherUserEmail, DocsFlowAppFixture.OtherUserPassword);

        using var response = await stranger.GetAsync($"/api/spaces/{foreign.Id}/contexts", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Access_granted_by_the_owner_makes_the_space_visible()
    {
        using var owner = fixture.CreateClient();
        await LogInAsync(owner);
        var space = await CreateSpaceAsync(owner, "Общий архив");

        using var invited = fixture.CreateClient();
        await LogInAsync(invited, DocsFlowAppFixture.OtherUserEmail, DocsFlowAppFixture.OtherUserPassword);
        var invitedId = (await ReadMeAsync(invited)).Id;

        using (var forbidden = await invited.GetAsync($"/api/spaces/{space.Id}", Ct))
        {
            forbidden.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        await AddMemberAsync(owner, space.Id, invitedId);

        using (var allowed = await invited.GetAsync($"/api/spaces/{space.Id}", Ct))
        {
            allowed.StatusCode.ShouldBe(HttpStatusCode.OK);

            var seen = await allowed.Content.ReadFromJsonAsync<SpaceResponse>(Ct);
            seen!.Role.ShouldBe("member");
        }

        // Отзыв возвращает всё как было — и снова неотличимо от несуществующего space.
        using var revoked = await owner.DeleteAsync($"/api/spaces/{space.Id}/members/{invitedId}", Ct);
        revoked.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var afterRevoke = await invited.GetAsync($"/api/spaces/{space.Id}", Ct);
        afterRevoke.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_member_creates_contexts_and_the_owner_sees_them()
    {
        using var owner = fixture.CreateClient();
        await LogInAsync(owner);
        var space = await CreateSpaceAsync(owner, "Совместный архив");

        using var member = fixture.CreateClient();
        await LogInAsync(member, DocsFlowAppFixture.OtherUserEmail, DocsFlowAppFixture.OtherUserPassword);
        await AddMemberAsync(owner, space.Id, (await ReadMeAsync(member)).Id);

        // Участник — полноправный соавтор: контексты общие, персональных и скрытых нет.
        await CreateContextAsync(member, space.Id, "Ремонт");

        var seenByOwner = await ListContextsAsync(owner, space.Id);

        seenByOwner.ShouldHaveSingleItem().Name.ShouldBe("Ремонт");
    }

    [Fact]
    public async Task A_member_cannot_rename_the_space()
    {
        using var owner = fixture.CreateClient();
        await LogInAsync(owner);
        var space = await CreateSpaceAsync(owner, "Имя владельца");

        using var member = fixture.CreateClient();
        await LogInAsync(member, DocsFlowAppFixture.OtherUserEmail, DocsFlowAppFixture.OtherUserPassword);
        await AddMemberAsync(owner, space.Id, (await ReadMeAsync(member)).Id);

        using var response = await member.SendJsonAsync(
            HttpMethod.Patch,
            $"/api/spaces/{space.Id}",
            new { name = "Имя участника" },
            Ct);

        // 403, а не 404: про существование этого space участник уже знает — он в нём состоит.
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_owner_renames_the_space()
    {
        using var client = fixture.CreateClient();
        await LogInAsync(client);
        var space = await CreateSpaceAsync(client, "Старое имя");

        using (var response = await client.SendJsonAsync(
            HttpMethod.Patch,
            $"/api/spaces/{space.Id}",
            new { name = "Новое имя" },
            Ct))
        {
            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        using var reread = await client.GetAsync($"/api/spaces/{space.Id}", Ct);
        var renamed = await reread.Content.ReadFromJsonAsync<SpaceResponse>(Ct);

        renamed!.Name.ShouldBe("Новое имя");
    }

    [Fact]
    public async Task An_unknown_user_cannot_be_invited()
    {
        using var client = fixture.CreateClient();
        await LogInAsync(client);
        var space = await CreateSpaceAsync(client, "Архив без гостей");

        using var response = await client.SendJsonAsync(
            HttpMethod.Post,
            $"/api/spaces/{space.Id}/members",
            new { userId = Guid.CreateVersion7() },
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_owner_cannot_be_excluded()
    {
        using var client = fixture.CreateClient();
        await LogInAsync(client);
        var space = await CreateSpaceAsync(client, "Архив владельца");
        var ownerId = (await ReadMeAsync(client)).Id;

        using var response = await client.DeleteAsync($"/api/spaces/{space.Id}/members/{ownerId}", Ct);

        // Space остался бы без владельца, а передачи владения нет.
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task The_members_list_shows_both_roles()
    {
        using var owner = fixture.CreateClient();
        await LogInAsync(owner);
        var space = await CreateSpaceAsync(owner, "Архив с участником");

        using var member = fixture.CreateClient();
        await LogInAsync(member, DocsFlowAppFixture.OtherUserEmail, DocsFlowAppFixture.OtherUserPassword);
        var memberId = (await ReadMeAsync(member)).Id;
        await AddMemberAsync(owner, space.Id, memberId);

        using var response = await owner.GetAsync($"/api/spaces/{space.Id}/members", Ct);
        var members = await response.Content.ReadFromJsonAsync<IReadOnlyList<MemberResponse>>(Ct);

        members!.Count.ShouldBe(2);
        members.Single(candidate => candidate.UserId == memberId).Role.ShouldBe("member");
        members.Count(candidate => candidate.Role == "owner").ShouldBe(1);
    }

    [Fact]
    public async Task A_context_name_is_taken_only_once_per_space()
    {
        using var client = fixture.CreateClient();
        await LogInAsync(client);
        var space = await CreateSpaceAsync(client, "Архив с контекстами");

        await CreateContextAsync(client, space.Id, "Финансы");

        using var duplicate = await client.SendJsonAsync(
            HttpMethod.Post,
            $"/api/spaces/{space.Id}/contexts",
            new { name = "финансы" },
            Ct);

        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_space_without_contexts_returns_an_empty_list()
    {
        using var client = fixture.CreateClient();
        await LogInAsync(client);
        var space = await CreateSpaceAsync(client, "Архив без контекстов");

        // Контексты необязательны: пустой список — нормальное состояние, а не 404.
        (await ListContextsAsync(client, space.Id)).ShouldBeEmpty();
    }

    private Task LogInAsync(BrowserClient client) =>
        LogInAsync(client, DocsFlowAppFixture.UserEmail, DocsFlowAppFixture.UserPassword);

    private Task LogInAsync(BrowserClient client, string email, string password) =>
        client.LogInAsync(fixture.BaseAddress, email, password, Ct);

    private static async Task<Me> ReadMeAsync(BrowserClient client)
    {
        using var response = await client.GetAsync("/api/me", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<Me>(Ct))!;
    }

    private static async Task<IReadOnlyList<SpaceResponse>> ListSpacesAsync(BrowserClient client)
    {
        using var response = await client.GetAsync("/api/spaces", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<SpaceResponse>>(Ct))!;
    }

    private static async Task<SpaceResponse> CreateSpaceAsync(BrowserClient client, string name)
    {
        using var response = await client.SendJsonAsync(HttpMethod.Post, "/api/spaces", new { name }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<SpaceResponse>(Ct))!;
    }

    private static async Task AddMemberAsync(BrowserClient owner, Guid spaceId, Guid userId)
    {
        using var response = await owner.SendJsonAsync(
            HttpMethod.Post,
            $"/api/spaces/{spaceId}/members",
            new { userId },
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private static async Task<ContextResponse> CreateContextAsync(BrowserClient client, Guid spaceId, string name)
    {
        using var response = await client.SendJsonAsync(
            HttpMethod.Post,
            $"/api/spaces/{spaceId}/contexts",
            new { name },
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<ContextResponse>(Ct))!;
    }

    private static async Task<IReadOnlyList<ContextResponse>> ListContextsAsync(BrowserClient client, Guid spaceId)
    {
        using var response = await client.GetAsync($"/api/spaces/{spaceId}/contexts", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<IReadOnlyList<ContextResponse>>(Ct))!;
    }
}
