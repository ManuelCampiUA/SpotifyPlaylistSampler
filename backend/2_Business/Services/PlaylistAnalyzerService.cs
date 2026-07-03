using System.Text.Json;
using backend.Domain;
using backend.Domain.Interfaces;
using backend.Business.DTOs;
using backend.Business.Interfaces;

namespace backend.Business.Services;

public partial class PlaylistAnalyzerService(ISpotifyService spotifyService, IPlaylistRepository repository)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    public async Task<PlaylistResultDto> AnalyzeAsync(string url, CancellationToken ct)
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
        var match = SpotifyUrlRegex().Match(url);
        if (!match.Success)
            throw new ArgumentException("URL Spotify non valido. Formato atteso: https://open.spotify.com/playlist/{id}");
        return match.Groups[1].Value;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"open\.spotify\.com/playlist/([A-Za-z0-9]+)")]
    private static partial System.Text.RegularExpressions.Regex SpotifyUrlRegex();

    public async Task<List<PlaylistSummaryDto>> GetHistoryAsync(CancellationToken ct)
    {
        var all = await repository.GetAllAsync(ct);
        return all.Select(p =>
        {
            var result = JsonSerializer.Deserialize<PlaylistResultDto>(p.ResultJson);
            return new PlaylistSummaryDto(
                p.SpotifyId,
                result?.PlaylistName ?? p.Name,
                result?.TotalTracks ?? 0,
                result?.ImageUrl
            );
        }).ToList();
    }

    public async Task<PlaylistResultDto?> GetPlaylistAsync(string spotifyId, CancellationToken ct)
    {
        var cached = await repository.GetBySpotifyIdAsync(spotifyId, ct);
        if (cached is null) return null;
        return JsonSerializer.Deserialize<PlaylistResultDto>(cached.ResultJson);
    }
}
