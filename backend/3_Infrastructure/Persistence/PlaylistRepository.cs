using backend.Domain;
using backend.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public class PlaylistRepository(AppDbContext db) : IPlaylistRepository
{
    public Task<PlaylistCache?> GetBySpotifyIdAsync(string spotifyId, string userId, CancellationToken ct = default)
        => db.Playlists
            .Where(p => p.SpotifyId == spotifyId && p.UserSpotifyId == userId)
            .OrderByDescending(p => p.AnalyzedAt)
            .FirstOrDefaultAsync(ct);

    public async Task SaveAsync(PlaylistCache playlist, CancellationToken ct = default)
    {
        db.Playlists.Add(playlist);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<PlaylistCache>> GetAllAsync(string userId, CancellationToken ct = default)
        => await db.Playlists
            .Where(p => p.UserSpotifyId == userId)
            .GroupBy(p => p.SpotifyId)
            .Select(g => g.OrderByDescending(p => p.AnalyzedAt).First())
            .ToListAsync(ct);

    public async Task DeleteAsync(string spotifyId, string userId, CancellationToken ct = default)
    {
        var entries = await db.Playlists
            .Where(p => p.SpotifyId == spotifyId && p.UserSpotifyId == userId)
            .ToListAsync(ct);
        db.Playlists.RemoveRange(entries);
        await db.SaveChangesAsync(ct);
    }
}
