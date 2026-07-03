using backend.Domain.Interfaces;

namespace backend.Infrastructure.Auth;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string SpotifyId =>
        httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
        ?? httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("Utente non autenticato.");
}
