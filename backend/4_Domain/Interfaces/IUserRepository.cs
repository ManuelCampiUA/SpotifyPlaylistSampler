namespace backend.Domain.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> GetBySpotifyIdAsync(string spotifyId, CancellationToken ct = default);
    Task SaveAsync(AppUser user, CancellationToken ct = default);
}
