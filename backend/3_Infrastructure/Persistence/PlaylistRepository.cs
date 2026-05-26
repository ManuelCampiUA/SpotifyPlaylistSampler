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
}
