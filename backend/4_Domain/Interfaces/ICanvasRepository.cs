using backend.Domain;

namespace backend.Domain.Interfaces;

public interface ICanvasRepository
{
    Task<List<CanvasNode>> GetAllNodesAsync(CancellationToken ct = default);

    Task<CanvasNode?> GetNodeByIdAsync(int id, CancellationToken ct = default);
    Task<CanvasNode?> GetNodeByReferenceIdAsync(string referenceId, CancellationToken ct = default);
    Task<List<CanvasNode>> GetNodesByIdsAsync(List<int> ids, CancellationToken ct = default);

    Task AddNodeAsync(CanvasNode node, CancellationToken ct = default);
    Task AddNodesAsync(IEnumerable<CanvasNode> nodes, CancellationToken ct = default);

    Task UpdateNodeAsync(CanvasNode node, CancellationToken ct = default);
    Task UpdateNodesAsync(List<CanvasNode> nodes, CancellationToken ct = default);

    Task RemoveNodeAsync(int nodeId, CancellationToken ct = default);
    Task RemovePlaylistNodesAsync(string playlistSpotifyId, CancellationToken ct = default);

    // ── Edges
    Task<List<CanvasEdge>> GetAllEdgesAsync(CancellationToken ct = default);
    Task AddEdgeAsync(CanvasEdge edge, CancellationToken ct = default);
    Task RemoveEdgeAsync(int edgeId, CancellationToken ct = default);
}
