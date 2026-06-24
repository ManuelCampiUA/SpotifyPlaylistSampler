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

    public Task<List<CanvasNode>> GetNodesByIdsAsync(List<int> ids, CancellationToken ct = default)
        => db.CanvasNodes.Where(n => ids.Contains(n.Id)).ToListAsync(ct);

    public async Task AddNodeAsync(CanvasNode node, CancellationToken ct = default)
    {
        db.CanvasNodes.Add(node);
        await db.SaveChangesAsync(ct);
    }

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

    public async Task UpdateNodesAsync(List<CanvasNode> nodes, CancellationToken ct = default)
    {
        db.CanvasNodes.UpdateRange(nodes);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveNodeAsync(int nodeId, CancellationToken ct = default)
    {
        var node = await db.CanvasNodes.FindAsync([nodeId], ct);
        if (node is not null)
        {
            db.CanvasNodes.Remove(node);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task RemovePlaylistNodesAsync(string playlistSpotifyId, CancellationToken ct = default)
    {
        var nodes = await db.CanvasNodes
            .Where(n => n.ParentPlaylistId == playlistSpotifyId)
            .ToListAsync(ct);

        db.CanvasNodes.RemoveRange(nodes);
        await db.SaveChangesAsync(ct);
    }

    public async Task ClearAllAsync(CancellationToken ct = default)
    {
        db.CanvasEdges.RemoveRange(db.CanvasEdges);
        db.CanvasNodes.RemoveRange(db.CanvasNodes);
        await db.SaveChangesAsync(ct);
    }

    // ── Edges

    public Task<List<CanvasEdge>> GetAllEdgesAsync(CancellationToken ct = default)
        => db.CanvasEdges.AsNoTracking().ToListAsync(ct);

    public async Task AddEdgeAsync(CanvasEdge edge, CancellationToken ct = default)
    {
        db.CanvasEdges.Add(edge);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveEdgeAsync(int edgeId, CancellationToken ct = default)
    {
        var edge = await db.CanvasEdges.FindAsync([edgeId], ct);
        if (edge is not null)
        {
            db.CanvasEdges.Remove(edge);
            await db.SaveChangesAsync(ct);
        }
    }
}
