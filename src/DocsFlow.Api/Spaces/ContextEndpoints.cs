using DocsFlow.Spaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DocsFlow.Api.Spaces;

internal static class ContextEndpoints
{
    public sealed record ContextResponse(Guid Id, string Name, DateTime CreatedAt);

    public sealed record ContextNameRequest(string? Name);

    /// <summary>
    /// Контексты живут внутри space, поэтому и маршруты у них вложенные: группа приходит сюда
    /// уже с проверкой членства, и отдельного способа добраться до контекста по его
    /// идентификатору — в обход space — нет.
    /// </summary>
    public static RouteGroupBuilder MapContextEndpoints(this RouteGroupBuilder space)
    {
        var contexts = space.MapGroup("/contexts");

        contexts.MapGet("/", async Task<Ok<IReadOnlyList<ContextResponse>>> (
            HttpContext http,
            IContextRepository repository,
            CancellationToken cancellationToken) =>
        {
            var found = await repository.ListAsync(SpaceAccess.Of(http).SpaceId, cancellationToken);

            // Пустой список — нормальное состояние space, а не ошибка: контексты необязательны.
            IReadOnlyList<ContextResponse> response = [.. found.Select(context => new ContextResponse(
                context.Id,
                context.Name,
                context.CreatedAt))];

            return TypedResults.Ok(response);
        });

        contexts.MapPost("/",
            async Task<Results<Created<ContextResponse>, Conflict<ProblemDetailsBody>, ValidationProblem>> (
            ContextNameRequest request,
            HttpContext http,
            IContextRepository repository,
            CancellationToken cancellationToken) =>
        {
            var access = SpaceAccess.Of(http);

            // Роль не проверяется намеренно: участник — полноправный соавтор, и контексты в space
            // общие. Персональных и скрытых контекстов нет.
            if (!SpaceNaming.TryNormalize(request.Name, out var name))
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = [$"Имя обязательно и не длиннее {SpaceNaming.MaxNameLength} символов."],
                });
            }

            var created = await repository.CreateAsync(access.SpaceId, name, cancellationToken);

            return created is null
                ? TypedResults.Conflict(new ProblemDetailsBody(
                    "Имя занято",
                    "Контекст с таким именем в этом space уже есть."))
                : TypedResults.Created(
                    $"/api/spaces/{access.SpaceId}/contexts/{created.Id}",
                    new ContextResponse(created.Id, created.Name, created.CreatedAt));
        });

        return space;
    }
}
