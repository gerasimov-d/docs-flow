using DocsFlow.Api.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DocsFlow.Api.Endpoints;

internal static class MeEndpoints
{
    /// <summary>Профиль текущего пользователя. Ролей и прав в системе нет, поэтому их здесь и нет.</summary>
    public sealed record MeResponse(Guid Id, string Email, string? DisplayName);

    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/me", async Task<Results<Ok<MeResponse>, UnauthorizedHttpResult>> (
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            var user = await currentUser.GetAsync(cancellationToken);

            // Cookie валидна, но записи в базе нет — например, аккаунт удалили между запросами.
            return user is null
                ? TypedResults.Unauthorized()
                : TypedResults.Ok(new MeResponse(user.Id, user.Email, user.DisplayName));
        })
        .RequireAuthorization();

        return endpoints;
    }
}
