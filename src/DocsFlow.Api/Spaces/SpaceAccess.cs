using DocsFlow.Api.Authentication;
using DocsFlow.Spaces;

namespace DocsFlow.Api.Spaces;

/// <summary>
/// Подтверждённый доступ к space: пользователь состоит в нём, роль известна. Кладётся в запрос
/// фильтром <see cref="SpaceMembershipFilter"/> и достаётся из него <see cref="Of"/>.
/// </summary>
/// <param name="SpaceId">
/// Идентификатор из маршрута, уже сверенный с членством. Идентификатору из тела запроса доверять
/// нельзя — сюда он не попадает вовсе.
/// </param>
internal sealed record SpaceAccess(Guid SpaceId, Guid UserId, SpaceRole Role)
{
    private const string HttpContextItemKey = "docsflow.space-access";

    public bool IsOwner => Role == SpaceRole.Owner;

    /// <summary>
    /// Отдаёт доступ, проверенный фильтром группы.
    /// </summary>
    /// <remarks>
    /// Параметром обработчика этот тип не сделать: минимальные API привязывают параметры до того,
    /// как выполнится хоть один фильтр, поэтому привязка не увидела бы результата проверки.
    /// Гарантию даёт не место вызова, а фильтр на группе space — под ним находятся все маршруты
    /// с <c>{spaceId}</c>, и новый эндпоинт получает проверку по факту объявления.
    /// </remarks>
    public static SpaceAccess Of(HttpContext context) =>
        context.Items[HttpContextItemKey] as SpaceAccess
        // Не «ссылка на null» где-то в середине обработчика, а внятная причина: эндпоинт объявлен
        // вне группы, на которой висит проверка членства.
        ?? throw new InvalidOperationException(
            $"Эндпоинт {context.Request.Path} читает {nameof(SpaceAccess)}, но не проходит через "
            + $"{nameof(SpaceMembershipFilter)}. Такие эндпоинты объявляются только внутри группы space.");

    internal static void Attach(HttpContext context, SpaceAccess access) =>
        context.Items[HttpContextItemKey] = access;
}

/// <summary>
/// Проверяет членство текущего пользователя в space из маршрута — единственная точка, где это
/// решение принимается. Вешается на группу целиком, чтобы новый эндпоинт получал проверку по факту
/// объявления, а не по внимательности автора.
/// </summary>
internal sealed class SpaceMembershipFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var http = context.HttpContext;

        var currentUser = http.RequestServices.GetRequiredService<ICurrentUser>();
        var spaces = http.RequestServices.GetRequiredService<ISpaceRepository>();

        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (http.Request.RouteValues["spaceId"] is not string raw || !Guid.TryParse(raw, out var spaceId))
        {
            return TypedResults.NotFound();
        }

        var role = await spaces.FindRoleAsync(spaceId, userId, http.RequestAborted);

        if (role is null)
        {
            // Ровно тот же ответ, что и на несуществующий space: по коду ответа нельзя узнать,
            // существует ли чужой space. Отличать 403 от 404 здесь означало бы раскрывать это.
            return TypedResults.NotFound();
        }

        SpaceAccess.Attach(http, new SpaceAccess(spaceId, userId, role.Value));

        return await next(context);
    }
}
