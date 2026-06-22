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

        // 1. Fetch playlist metadata + first page of tracks
        var playlist = await client.Playlists.Get(playlistId, ct);

        // 2. Paginate through ALL tracks (handles playlists > 100 items)
        var allItems = await client.PaginateAll(playlist.Items!, cancellationToken: ct);

        var tracks = allItems
            .Select(item => item.Track)
            .OfType<FullTrack>()
            .ToList();

        // 3. Collect unique artist IDs across all tracks
        var artistIds = tracks
            .SelectMany(t => t.Artists)
            .Select(a => a.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        // 4. Fetch full artist details individually (batch endpoint removed by Spotify).
        //    Tasks run concurrently, capped at 10 parallel requests to respect rate limits.
        var fullArtists = new List<FullArtist>();
        foreach (var batch in artistIds.Chunk(10))
        {
            var tasks = batch.Select(id => client.Artists.Get(id, ct));
            var results = await Task.WhenAll(tasks);
            fullArtists.AddRange(results.Where(a => a is not null));
        }


        // 5. Collect all unique genres across the entire playlist
        var allGenres = fullArtists
            .SelectMany(a => a.Genres ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g)
            .ToList();

        // 6. Map to DTOs
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
