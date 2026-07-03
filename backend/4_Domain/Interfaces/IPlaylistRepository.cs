using backend.Domain;

namespace backend.Domain.Interfaces;

public interface IPlaylistRepository
{
    Task<PlaylistCache?> GetBySpotifyIdAsync(string spotifyId, string userId, CancellationToken ct = default);
    Task SaveAsync(PlaylistCache playlist, CancellationToken ct = default);
    Task<List<PlaylistCache>> GetAllAsync(string userId, CancellationToken ct = default);
    Task DeleteAsync(string spotifyId, string userId, CancellationToken ct = default);
}
