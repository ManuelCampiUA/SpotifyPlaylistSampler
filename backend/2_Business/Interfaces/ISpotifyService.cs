using backend.Business.DTOs;

namespace backend.Business.Interfaces;

public interface ISpotifyService
{
    Task<PlaylistResultDto> FetchPlaylistAsync(string playlistId, CancellationToken ct = default);
}
