using System.Text.Json;
using System.Text.RegularExpressions;
using backend.Domain;
using backend.Domain.Interfaces;
using backend.Business.DTOs;

namespace backend.Business.Services;

public partial class PlaylistAnalyzerService(ISpotifyService spotifyService, IPlaylistRepository repository)
{
    public async Task<PlaylistResultDto> AnalyzeAsync(string url, CancellationToken ct)
    {
        var playlistId = ExtractPlaylistId(url);
        var cached = await repository.GetBySpotifyIdAsync(playlistId, ct);

        TimeSpan CacheTtl = TimeSpan.FromHours(1);
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
        Regex spotifyRegex = new(@"open\.spotify\.com/playlist/([A-Za-z0-9]+)");
        var match = spotifyRegex.Match(url);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        else
        {
            throw new ArgumentException("URL Spotify non valido. Formato atteso: https://open.spotify.com/playlist/{id}");
        }
    }

    public async Task<List<PlaylistSummaryDto>> GetHistoryAsync(CancellationToken ct)
    {
        var all = await repository.GetAllAsync(ct);
        return all.Select(p => new PlaylistSummaryDto(p.SpotifyId, p.Name, p.AnalyzedAt)).ToList();
    }

    public async Task<PlaylistResultDto?> GetPlaylistAsync(string spotifyId, CancellationToken ct)
    {
        var cached = await repository.GetBySpotifyIdAsync(spotifyId, ct);
        if (cached is null) return null;
        return JsonSerializer.Deserialize<PlaylistResultDto>(cached.ResultJson);
    }
}
