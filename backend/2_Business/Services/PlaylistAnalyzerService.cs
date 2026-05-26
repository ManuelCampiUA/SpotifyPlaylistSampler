using System.Text.Json;
using System.Text.RegularExpressions;
using backend.Domain;
using backend.Domain.Interfaces;
using backend.Business.DTOs;

namespace backend.Business.Services;

public partial class PlaylistAnalyzerService(
    ISpotifyService spotifyService,
    IPlaylistRepository repository)
{
    // Cache is considered fresh for 24 hours.
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    // Matches: https://open.spotify.com/playlist/37i9dQZF1DXcBWIGoYBM5M
    [GeneratedRegex(@"open\.spotify\.com/playlist/([A-Za-z0-9]+)")]
    private static partial Regex PlaylistUrlRegex();

    public async Task<PlaylistResultDto> AnalyzeAsync(string url, CancellationToken ct = default)
    {
        var playlistId = ExtractPlaylistId(url);

        var cached = await repository.GetBySpotifyIdAsync(playlistId, ct);

        if (cached is not null && DateTime.UtcNow - cached.AnalyzedAt < CacheTtl)
        {
            return JsonSerializer.Deserialize<PlaylistResultDto>(cached.ResultJson)!;
        }

        var result = await spotifyService.FetchPlaylistAsync(playlistId, ct);

        await repository.SaveAsync(new PlaylistCache
        {
            SpotifyId = playlistId,
            Name = result.PlaylistName,
            AnalyzedAt = DateTime.UtcNow,
            ResultJson = JsonSerializer.Serialize(result)
        }, ct);

        return result;
    }

    private static string ExtractPlaylistId(string url)
    {
        var match = PlaylistUrlRegex().Match(url);
        if (!match.Success)
            throw new ArgumentException("URL Spotify non valido. Formato atteso: https://open.spotify.com/playlist/{id}");

        return match.Groups[1].Value;
    }
}
