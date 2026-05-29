using backend.Domain;
using backend.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public class PlaylistRepository(AppDbContext db) : IPlaylistRepository
{
    public Task<PlaylistCache?> GetBySpotifyIdAsync(string spotifyId, CancellationToken ct = default)
        => db.Playlists
            .Where(p => p.SpotifyId == spotifyId)
            .OrderByDescending(p => p.AnalyzedAt)
            .FirstOrDefaultAsync(ct);

    public async Task SaveAsync(PlaylistCache playlist, CancellationToken ct = default)
    {
        db.Playlists.Add(playlist);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<PlaylistCache>> GetAllAsync(CancellationToken ct = default)
        => await db.Playlists
            .GroupBy(p => p.SpotifyId)
            .Select(g => g.OrderByDescending(p => p.AnalyzedAt).First())
            .ToListAsync(ct);
}
