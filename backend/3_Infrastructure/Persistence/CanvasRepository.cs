using backend.Domain;
using backend.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public class CanvasRepository(AppDbContext db) : ICanvasRepository
{
    public Task<List<CanvasNode>> GetAllNodesAsync(CancellationToken ct = default)
        => db.CanvasNodes.AsNoTracking().ToListAsync(ct);

    public Task<CanvasNode?> GetNodeByIdAsync(int id, CancellationToken ct = default)
        => db.CanvasNodes.FirstOrDefaultAsync(n => n.Id == id, ct);

    public Task<CanvasNode?> GetNodeByReferenceIdAsync(string referenceId, CancellationToken ct = default)
        => db.CanvasNodes.FirstOrDefaultAsync(n => n.ReferenceId == referenceId, ct);

    public async Task AddNodesAsync(IEnumerable<CanvasNode> nodes, CancellationToken ct = default)
    {
        db.CanvasNodes.AddRange(nodes);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateNodeAsync(CanvasNode node, CancellationToken ct = default)
    {
        db.CanvasNodes.Update(node);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemovePlaylistNodesAsync(string playlistSpotifyId, CancellationToken ct = default)
    {
        var nodes = await db.CanvasNodes
            .Where(n => n.ParentPlaylistId == playlistSpotifyId)
            .ToListAsync(ct);

        db.CanvasNodes.RemoveRange(nodes);
        await db.SaveChangesAsync(ct);
    }
}
