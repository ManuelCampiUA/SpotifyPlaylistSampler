using backend.Domain;
using backend.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public class CanvasRepository(AppDbContext db) : ICanvasRepository
{
    public Task<List<CanvasNode>> GetAllNodesAsync(string userId, CancellationToken ct = default)
        => db.CanvasNodes.Where(n => n.UserSpotifyId == userId).AsNoTracking().ToListAsync(ct);

    public Task<CanvasNode?> GetNodeByIdAsync(int id, string userId, CancellationToken ct = default)
        => db.CanvasNodes.FirstOrDefaultAsync(n => n.Id == id && n.UserSpotifyId == userId, ct);

    public Task<CanvasNode?> GetNodeByReferenceIdAsync(string referenceId, string userId, CancellationToken ct = default)
        => db.CanvasNodes.FirstOrDefaultAsync(n => n.ReferenceId == referenceId && n.UserSpotifyId == userId, ct);

    public Task<List<CanvasNode>> GetNodesByIdsAsync(List<int> ids, string userId, CancellationToken ct = default)
        => db.CanvasNodes.Where(n => ids.Contains(n.Id) && n.UserSpotifyId == userId).ToListAsync(ct);

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

    public async Task RemoveNodeAsync(int nodeId, string userId, CancellationToken ct = default)
    {
        var node = await db.CanvasNodes.FirstOrDefaultAsync(n => n.Id == nodeId && n.UserSpotifyId == userId, ct);
        if (node is not null)
        {
            db.CanvasNodes.Remove(node);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task RemovePlaylistNodesAsync(string playlistSpotifyId, string userId, CancellationToken ct = default)
    {
        var nodes = await db.CanvasNodes
            .Where(n => n.ParentPlaylistId == playlistSpotifyId && n.UserSpotifyId == userId)
            .ToListAsync(ct);
        db.CanvasNodes.RemoveRange(nodes);
        await db.SaveChangesAsync(ct);
    }

    public async Task ClearAllAsync(string userId, CancellationToken ct = default)
    {
        var userEdges = await db.CanvasEdges
            .Where(e => e.SourceNode.UserSpotifyId == userId)
            .ToListAsync(ct);
        db.CanvasEdges.RemoveRange(userEdges);

        var userNodes = await db.CanvasNodes
            .Where(n => n.UserSpotifyId == userId)
            .ToListAsync(ct);
        db.CanvasNodes.RemoveRange(userNodes);
        await db.SaveChangesAsync(ct);
    }

    public Task<List<CanvasEdge>> GetAllEdgesAsync(string userId, CancellationToken ct = default)
        => db.CanvasEdges.Where(e => e.SourceNode.UserSpotifyId == userId).AsNoTracking().ToListAsync(ct);

    public async Task AddEdgeAsync(CanvasEdge edge, CancellationToken ct = default)
    {
        db.CanvasEdges.Add(edge);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveEdgeAsync(int edgeId, string userId, CancellationToken ct = default)
    {
        var edge = await db.CanvasEdges
            .Include(e => e.SourceNode)
            .FirstOrDefaultAsync(e => e.Id == edgeId && e.SourceNode.UserSpotifyId == userId, ct);
        if (edge is not null)
        {
            db.CanvasEdges.Remove(edge);
            await db.SaveChangesAsync(ct);
        }
    }
}
