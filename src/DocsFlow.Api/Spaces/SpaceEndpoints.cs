using DocsFlow.Api.Authentication;
using DocsFlow.Spaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DocsFlow.Api.Spaces;

internal static class SpaceEndpoints
{
    /// <summary>Space вместе с ролью того, кто его запросил.</summary>
    public sealed record SpaceResponse(Guid Id, string Name, string Role, DateTime CreatedAt);

    /// <summary>Участник space. Состав видят только свои.</summary>
    public sealed record MemberResponse(Guid UserId, string Email, string? DisplayName, string Role);

    public sealed record SpaceNameRequest(string? Name);

    /// <summary>
    /// Кого добавляем. Приглашение идёт по внутреннему идентификатору, а не по email: ответ на
    /// незнакомый адрес показывал бы, заведён ли в сервисе аккаунт с таким email.
    /// </summary>
    public sealed record AddMemberRequest(Guid UserId);

    public static IEndpointRouteBuilder MapSpaceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var spaces = endpoints.MapGroup("/api/spaces").RequireAuthorization();

        spaces.MapGet("/", async Task<Results<Ok<IReadOnlyList<SpaceResponse>>, UnauthorizedHttpResult>> (
            ICurrentUser currentUser,
            ISpaceRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } userId)
            {
                return TypedResults.Unauthorized();
            }

            var memberships = await repository.ListForUserAsync(userId, cancellationToken);

            IReadOnlyList<SpaceResponse> response = [.. memberships.Select(membership => new SpaceResponse(
                membership.Id,
                membership.Name,
                membership.Role.ToWire(),
                membership.CreatedAt))];

            return TypedResults.Ok(response);
        });

        spaces.MapPost("/", async Task<Results<Created<SpaceResponse>, ValidationProblem, UnauthorizedHttpResult>> (
            SpaceNameRequest request,
            ICurrentUser currentUser,
            ISpaceRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } userId)
            {
                return TypedResults.Unauthorized();
            }

            if (!SpaceNaming.TryNormalize(request.Name, out var name))
            {
                return InvalidName();
            }

            var space = await repository.CreateAsync(userId, name, cancellationToken);

            return TypedResults.Created(
                $"/api/spaces/{space.Id}",
                new SpaceResponse(space.Id, space.Name, SpaceRole.Owner.ToWire(), space.CreatedAt));
        });

        // Всё, что дальше, работает с конкретным space — и потому проходит проверку членства.
        // Фильтр висит на группе: эндпоинт получает проверку от того, что объявлен здесь.
        var space = spaces.MapGroup("/{spaceId:guid}").AddEndpointFilter<SpaceMembershipFilter>();

        space.MapGet("/", async Task<Results<Ok<SpaceResponse>, NotFound>> (
            HttpContext http,
            ISpaceRepository repository,
            CancellationToken cancellationToken) =>
        {
            var access = SpaceAccess.Of(http);

            var found = await repository.FindAsync(access.SpaceId, cancellationToken);

            // Членство есть, а space нет — его успели удалить между проверкой и чтением.
            return found is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(new SpaceResponse(
                    found.Id,
                    found.Name,
                    access.Role.ToWire(),
                    found.CreatedAt));
        });

        space.MapPatch("/", async Task<Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem>> (
            SpaceNameRequest request,
            HttpContext http,
            ISpaceRepository repository,
            CancellationToken cancellationToken) =>
        {
            var access = SpaceAccess.Of(http);

            // 403, а не 404: про существование этого space спрашивающий уже знает — он в нём состоит.
            if (!access.IsOwner)
            {
                return TypedResults.Forbid();
            }

            if (!SpaceNaming.TryNormalize(request.Name, out var name))
            {
                return InvalidName();
            }

            return await repository.RenameAsync(access.SpaceId, name, cancellationToken)
                ? TypedResults.NoContent()
                : TypedResults.NotFound();
        });

        space.MapGet("/members", async Task<Ok<IReadOnlyList<MemberResponse>>> (
            HttpContext http,
            ISpaceRepository repository,
            CancellationToken cancellationToken) =>
        {
            var access = SpaceAccess.Of(http);

            var members = await repository.ListMembersAsync(access.SpaceId, cancellationToken);

            IReadOnlyList<MemberResponse> response = [.. members.Select(member => new MemberResponse(
                member.UserId,
                member.Email,
                member.DisplayName,
                member.Role.ToWire()))];

            return TypedResults.Ok(response);
        });

        space.MapPost("/members", async Task<Results<NoContent, NotFound<ProblemDetailsBody>, ForbidHttpResult>> (
            AddMemberRequest request,
            HttpContext http,
            ISpaceRepository repository,
            CancellationToken cancellationToken) =>
        {
            var access = SpaceAccess.Of(http);

            if (!access.IsOwner)
            {
                return TypedResults.Forbid();
            }

            var result = await repository.AddMemberAsync(access.SpaceId, request.UserId, cancellationToken);

            return result switch
            {
                // Повторная выдача доступа — не ошибка: результат ровно тот, которого просили.
                AddMemberResult.Added or AddMemberResult.AlreadyMember => TypedResults.NoContent(),
                _ => TypedResults.NotFound(new ProblemDetailsBody(
                    "Пользователь не найден",
                    "Приглашать можно только зарегистрированных пользователей.")),
            };
        });

        space.MapDelete("/members/{userId:guid}",
            async Task<Results<NoContent, Conflict<ProblemDetailsBody>, ForbidHttpResult>> (
            Guid userId,
            HttpContext http,
            ISpaceRepository repository,
            CancellationToken cancellationToken) =>
        {
            var access = SpaceAccess.Of(http);

            if (!access.IsOwner)
            {
                return TypedResults.Forbid();
            }

            var result = await repository.RemoveMemberAsync(access.SpaceId, userId, cancellationToken);

            return result switch
            {
                // Отзыв у того, кто и так не состоит, оставляет ровно то состояние, которого просили.
                RemoveMemberResult.Removed or RemoveMemberResult.NotMember => TypedResults.NoContent(),
                _ => TypedResults.Conflict(new ProblemDetailsBody(
                    "Владельца нельзя исключить",
                    "Space остался бы без владельца: передача владения не поддерживается.")),
            };
        });

        space.MapContextEndpoints();

        return endpoints;
    }

    private static ValidationProblem InvalidName() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["name"] = [$"Имя обязательно и не длиннее {SpaceNaming.MaxNameLength} символов."],
        });
}

/// <summary>Тело ответа об ошибке — короткое и без внутренних подробностей.</summary>
internal sealed record ProblemDetailsBody(string Title, string Detail);
