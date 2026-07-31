using DocsFlow.Users;

namespace DocsFlow.Api.Authentication;

internal sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _users;

    private User? _cached;

    public CurrentUser(IHttpContextAccessor httpContextAccessor, IUserRepository users)
    {
        _httpContextAccessor = httpContextAccessor;
        _users = users;
    }

    public Guid? UserId =>
        Guid.TryParse(
            _httpContextAccessor.HttpContext?.User.FindFirst(DocsFlowClaims.UserId)?.Value,
            out var userId)
            ? userId
            : null;

    public async Task<User?> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        // Сервис scoped, поэтому запрос к базе делается не больше одного раза за HTTP-запрос.
        return UserId is { } userId
            ? _cached = await _users.GetByIdAsync(userId, cancellationToken)
            : null;
    }
}
