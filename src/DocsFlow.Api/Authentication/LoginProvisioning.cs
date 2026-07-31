using System.Security.Claims;
using DocsFlow.Spaces;
using DocsFlow.Users;
using Microsoft.AspNetCore.Authentication;

namespace DocsFlow.Api.Authentication;

/// <summary>
/// Заводит пользователя в нашей базе в момент входа. Вешается на <c>OnTicketReceived</c>: claims
/// уже проверены, cookie ещё не записана — значит, в неё можно добавить внутренний идентификатор.
/// </summary>
internal static class LoginProvisioning
{
    /// <summary>
    /// Имя первого space. Нейтральное и переименовываемое: угадывать за пользователя, как он
    /// назовёт своё пространство, смысла нет, а состояние «войти можно, а грузить некуда»
    /// недопустимо.
    /// </summary>
    private const string FirstSpaceName = "Личное";

    public static async Task OnTicketReceivedAsync(TicketReceivedContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<TicketReceivedContext>>();

        if (context.Principal?.Identity is not ClaimsIdentity identity)
        {
            Reject(context, logger, "в тикете нет ClaimsIdentity");
            return;
        }

        var subject = context.Principal.FindFirst(OidcClaims.Subject)?.Value;
        var email = context.Principal.FindFirst(OidcClaims.Email)?.Value;

        if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(email))
        {
            Reject(context, logger, "в токене нет claim'ов sub или email");
            return;
        }

        // Второй рубеж: Keycloak с включённым подтверждением email до сюда не пустит. Если
        // настройка realm однажды разойдётся с ожиданием, регистрация на чужой адрес не сработает.
        if (!bool.TryParse(context.Principal.FindFirst(OidcClaims.EmailVerified)?.Value, out var emailVerified)
            || !emailVerified)
        {
            Reject(context, logger, $"email {email} не подтверждён");
            return;
        }

        var identityFromToken = new ExternalIdentity(
            subject,
            email,
            context.Principal.FindFirst(OidcClaims.Name)?.Value
                ?? context.Principal.FindFirst(OidcClaims.PreferredUsername)?.Value,
            EmailVerified: true);

        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var user = await users.UpsertBySubjectAsync(identityFromToken, context.HttpContext.RequestAborted);

        // Первый space заводится здесь же, а не при первой загрузке документа: иначе между
        // регистрацией и загрузкой существует состояние «залогинен, но грузить некуда». Метод
        // идемпотентен, поэтому на каждом следующем входе он ничего не делает. Ошибка отсюда
        // всплывает и отменяет вход — пустить пользователя без space хуже, чем не пустить вовсе.
        var spaces = context.HttpContext.RequestServices.GetRequiredService<ISpaceRepository>();
        var firstSpace = await spaces.CreateFirstIfMissingAsync(
            user.Id,
            FirstSpaceName,
            context.HttpContext.RequestAborted);

        if (firstSpace is not null)
        {
            logger.LogInformation(
                "Пользователю {UserId} создан первый space {SpaceId}.",
                user.Id,
                firstSpace.Id);
        }

        identity.AddClaim(new Claim(DocsFlowClaims.UserId, user.Id.ToString()));
    }

    private static void Reject(TicketReceivedContext context, ILogger logger, string reason)
    {
        logger.LogWarning("Вход отклонён: {Reason}", reason);

        // HandleResponse останавливает штатную обработку — cookie не выдаётся, редиректа нет.
        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
    }
}
