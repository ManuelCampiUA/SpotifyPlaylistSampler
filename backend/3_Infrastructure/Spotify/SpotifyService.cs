using backend.Business.DTOs;
using backend.Domain.Interfaces;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;

namespace backend.Infrastructure.Spotify;

public class SpotifyService(IOptions<SpotifyOptions> options) : ISpotifyService
{
    public async Task<PlaylistResultDto> FetchPlaylistAsync(string playlistId, CancellationToken ct = default)
    {
        var client = BuildClient();

        var playlist = await client.Playlists.Get(playlistId, ct);
        var allItems = await client.PaginateAll(playlist.Items!, cancellationToken: ct);

        var tracks = allItems
            .Select(item => item.Track)
            .OfType<FullTrack>()
            .ToList();

        var artistIds = tracks
            .SelectMany(t => t.Artists)
            .Select(a => a.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var fullArtists = new List<FullArtist>();
        foreach (var batch in artistIds.Chunk(10))
        {
            var tasks = batch.Select(id => client.Artists.Get(id, ct));
            var results = await Task.WhenAll(tasks);
            fullArtists.AddRange(results.Where(a => a is not null));
        }

        var allGenres = fullArtists
            .SelectMany(a => a.Genres ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g)
            .ToList();

        var trackDtos = tracks.Select(t => new TrackDto(
            Name: t.Name,
            Artists: [.. t.Artists.Select(a => a.Name)],
            DurationMs: t.DurationMs,
            PreviewUrl: t.PreviewUrl
        )).ToList();

        return new PlaylistResultDto(
            PlaylistName: playlist.Name ?? string.Empty,
            Description: string.IsNullOrWhiteSpace(playlist.Description) ? null : playlist.Description,
            TotalTracks: tracks.Count,
            ImageUrl: playlist.Images?.FirstOrDefault()?.Url,
            Tracks: trackDtos,
            Genres: allGenres
        );
    }

    private SpotifyClient BuildClient()
    {
        var config = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new ClientCredentialsAuthenticator(
                options.Value.ClientId,
                options.Value.ClientSecret));

        return new SpotifyClient(config);
    }
}
