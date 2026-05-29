using backend.Domain;

namespace backend.Domain.Interfaces;

public interface ICanvasRepository
{
    Task<List<CanvasNode>> GetAllNodesAsync(CancellationToken ct = default);
    Task<List<CanvasEdge>> GetAllEdgesAsync(CancellationToken ct = default);

    Task<CanvasNode?> GetNodeByIdAsync(int id, CancellationToken ct = default);
    Task<CanvasNode?> GetNodeByReferenceIdAsync(string referenceId, CancellationToken ct = default);

    Task AddNodesAsync(IEnumerable<CanvasNode> nodes, CancellationToken ct = default);
    Task AddEdgesAsync(IEnumerable<CanvasEdge> edges, CancellationToken ct = default);

    Task UpdateNodeAsync(CanvasNode node, CancellationToken ct = default);

    Task<CanvasEdge> AddEdgeAsync(CanvasEdge edge, CancellationToken ct = default);
    Task RemoveEdgeAsync(int edgeId, CancellationToken ct = default);

    /// <summary>Removes a node and all edges referencing it.</summary>
    Task RemoveNodeAsync(int nodeId, CancellationToken ct = default);

    /// <summary>Removes all nodes belonging to a playlist (including the playlist node itself) and their edges.</summary>
    Task RemovePlaylistNodesAsync(string playlistSpotifyId, CancellationToken ct = default);
}
