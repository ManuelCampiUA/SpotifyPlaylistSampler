using backend.Domain;

namespace backend.Domain.Interfaces;

public interface IPlaylistRepository
{
    /// <summary>Returns the most recent cached entry for this Spotify playlist ID, or null.</summary>
    Task<PlaylistCache?> GetBySpotifyIdAsync(string spotifyId, CancellationToken ct = default);

    /// <summary>Persists a new analysis result.</summary>
    Task SaveAsync(PlaylistCache playlist, CancellationToken ct = default);
}
