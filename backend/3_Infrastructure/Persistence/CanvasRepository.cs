using backend.Domain;
using backend.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public class CanvasRepository(AppDbContext db) : ICanvasRepository
{
    public Task<List<CanvasNode>> GetAllNodesAsync(CancellationToken ct = default)
        => db.CanvasNodes.AsNoTracking().ToListAsync(ct);

    public Task<List<CanvasEdge>> GetAllEdgesAsync(CancellationToken ct = default)
        => db.CanvasEdges.AsNoTracking().ToListAsync(ct);

    public Task<CanvasNode?> GetNodeByIdAsync(int id, CancellationToken ct = default)
        => db.CanvasNodes.FirstOrDefaultAsync(n => n.Id == id, ct);

    public Task<CanvasNode?> GetNodeByReferenceIdAsync(string referenceId, CancellationToken ct = default)
        => db.CanvasNodes.FirstOrDefaultAsync(n => n.ReferenceId == referenceId, ct);

    public async Task AddNodesAsync(IEnumerable<CanvasNode> nodes, CancellationToken ct = default)
    {
        db.CanvasNodes.AddRange(nodes);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddEdgesAsync(IEnumerable<CanvasEdge> edges, CancellationToken ct = default)
    {
        db.CanvasEdges.AddRange(edges);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateNodeAsync(CanvasNode node, CancellationToken ct = default)
    {
        db.CanvasNodes.Update(node);
        await db.SaveChangesAsync(ct);
    }

    public async Task<CanvasEdge> AddEdgeAsync(CanvasEdge edge, CancellationToken ct = default)
    {
        db.CanvasEdges.Add(edge);
        await db.SaveChangesAsync(ct);
        return edge;
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

    public async Task RemoveNodeAsync(int nodeId, CancellationToken ct = default)
    {
        var edges = await db.CanvasEdges
            .Where(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId)
            .ToListAsync(ct);
        db.CanvasEdges.RemoveRange(edges);

        var node = await db.CanvasNodes.FindAsync([nodeId], ct);
        if (node is not null)
            db.CanvasNodes.Remove(node);

        await db.SaveChangesAsync(ct);
    }

    public async Task RemovePlaylistNodesAsync(string playlistSpotifyId, CancellationToken ct = default)
    {
        var nodeIds = await db.CanvasNodes
            .Where(n => n.ReferenceId == playlistSpotifyId || n.ParentPlaylistId == playlistSpotifyId)
            .Select(n => n.Id)
            .ToListAsync(ct);

        var edges = await db.CanvasEdges
            .Where(e => nodeIds.Contains(e.SourceNodeId) || nodeIds.Contains(e.TargetNodeId))
            .ToListAsync(ct);
        db.CanvasEdges.RemoveRange(edges);

        var nodes = await db.CanvasNodes
            .Where(n => nodeIds.Contains(n.Id))
            .ToListAsync(ct);
        db.CanvasNodes.RemoveRange(nodes);

        await db.SaveChangesAsync(ct);
    }
}
