using backend.Business.DTOs;

namespace backend.Domain.Interfaces;

public interface ISpotifyService
{
    /// <summary>Fetches playlist data from the Spotify Web API.</summary>
    Task<PlaylistResultDto> FetchPlaylistAsync(string playlistId, CancellationToken ct = default);
}
