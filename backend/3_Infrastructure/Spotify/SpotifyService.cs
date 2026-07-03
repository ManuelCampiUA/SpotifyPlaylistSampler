using backend.Business.DTOs;
using backend.Business.Interfaces;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;

namespace backend.Infrastructure.Spotify;

public class SpotifyService(IOptions<SpotifyOptions> options) : ISpotifyService
{
    private readonly SpotifyClient _client = new(
        SpotifyClientConfig.CreateDefault()
            .WithAuthenticator(new ClientCredentialsAuthenticator(
                options.Value.ClientId,
                options.Value.ClientSecret)));

    public async Task<PlaylistResultDto> FetchPlaylistAsync(string playlistId, CancellationToken ct = default)
    {
        var playlist = await _client.Playlists.Get(playlistId, ct);
        var allItems = await _client.PaginateAll(playlist.Items!, cancellationToken: ct);

        var tracks = allItems
            .Select(item => item.Track)
            .OfType<FullTrack>()
            .ToList();

        List<string> artistIds = tracks
                    .SelectMany(t => t.Artists)
                    .Select(a => a.Id)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

        const int batchSize = 5;
        const int maxConcurrency = 3;
        const int delayBetweenBatchesMs = 200; // Delay tra batch per rispettare rate limits
        SemaphoreSlim semaphore = new(maxConcurrency);
        List<string[]> batches = [.. artistIds.Chunk(batchSize)];
        List<FullArtist> fullArtists = [];

        for (int i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            var batchTasks = new List<Task<FullArtist?>>();

            foreach (var artistId in batch)
            {
                await semaphore.WaitAsync(ct);

                batchTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        return await FetchArtistWithRetryAsync(_client, artistId, ct);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, ct));
            }

            var batchResults = await Task.WhenAll(batchTasks);
            fullArtists.AddRange(batchResults.Where(a => a is not null)!);

            // Aggiungo un delay tra i batch per rispettare i rate limits di Spotify
            if (i < batches.Count - 1)
            {
                await Task.Delay(delayBetweenBatchesMs, ct);
            }
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

    private static async Task<FullArtist?> FetchArtistWithRetryAsync(SpotifyClient client, string artistId, CancellationToken ct, int maxRetries = 3)
    {
        var retryCount = 0;
        var baseDelay = 500; // millisecondi

        while (retryCount < maxRetries)
        {
            try
            {
                return await client.Artists.Get(artistId, ct);
            }
            catch (APITooManyRequestsException ex)
            {
                // Rate limit hit - aspetto il tempo suggerito da Spotify
                var retryAfter = ex.RetryAfter;
                await Task.Delay(retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.FromSeconds(1), ct);
                retryCount++;
            }
            catch (APIException) when (retryCount < maxRetries - 1)
            {
                // Exponential backoff per altri errori API
                var delay = baseDelay * (int)Math.Pow(2, retryCount);
                await Task.Delay(delay, ct);
                retryCount++;
            }
            catch
            {
                // Per altri errori (network, etc.) ritorno null
                return null;
            }
        }

        return null;
    }
}
