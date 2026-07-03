using backend.Domain;

namespace backend.Domain.Interfaces;

public interface ICanvasRepository
{
    Task<List<CanvasNode>> GetAllNodesAsync(string userId, CancellationToken ct = default);

    Task<CanvasNode?> GetNodeByIdAsync(int id, string userId, CancellationToken ct = default);
    Task<CanvasNode?> GetNodeByReferenceIdAsync(string referenceId, string userId, CancellationToken ct = default);
    Task<List<CanvasNode>> GetNodesByIdsAsync(List<int> ids, string userId, CancellationToken ct = default);

    Task AddNodeAsync(CanvasNode node, CancellationToken ct = default);
    Task AddNodesAsync(IEnumerable<CanvasNode> nodes, CancellationToken ct = default);

    Task UpdateNodeAsync(CanvasNode node, CancellationToken ct = default);
    Task UpdateNodesAsync(List<CanvasNode> nodes, CancellationToken ct = default);

    Task RemoveNodeAsync(int nodeId, string userId, CancellationToken ct = default);
    Task RemovePlaylistNodesAsync(string playlistSpotifyId, string userId, CancellationToken ct = default);
    Task ClearAllAsync(string userId, CancellationToken ct = default);

    Task<List<CanvasEdge>> GetAllEdgesAsync(string userId, CancellationToken ct = default);
    Task AddEdgeAsync(CanvasEdge edge, CancellationToken ct = default);
    Task RemoveEdgeAsync(int edgeId, string userId, CancellationToken ct = default);
}
