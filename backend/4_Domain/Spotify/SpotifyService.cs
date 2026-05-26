using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;

namespace Backend.Infrastructure.Spotify;

public class SpotifyService(IOptions<SpotifyOptions> options) : ISpotifyService
{
    public async Task<PlaylistResultDto> FetchPlaylistAsync(string playlistId, CancellationToken ct = default)
    {
        var client = BuildClient();

        // 1. Fetch playlist metadata + first page of tracks
        var playlist = await client.Playlists.Get(playlistId);

        // 2. Paginate through ALL tracks (handles playlists > 100 items)
        var allItems = await client.PaginateAll(playlist.Tracks!);

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

        // 4. Fetch full artist details in batches of 50 (Spotify API limit)
        var fullArtists = new List<FullArtist>();
        foreach (var batch in artistIds.Chunk(50))
        {
            var response = await client.Artists.GetSeveral(new ArtistsRequest([.. batch]));
            fullArtists.AddRange(response.Artists.Where(a => a is not null));
        }

        var artistMap = fullArtists.ToDictionary(a => a.Id, a => a);

        // 5. Collect all unique genres across the entire playlist
        var allGenres = fullArtists
            .SelectMany(a => a.Genres ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g)
            .ToList();

        // 6. Map to DTOs
        var trackDtos = tracks.Select(t => new TrackDto(
            Name: t.Name,
            Artists: t.Artists.Select(a => a.Name).ToList(),
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
