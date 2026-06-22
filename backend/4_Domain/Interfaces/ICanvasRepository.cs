using backend.Domain;

namespace backend.Domain.Interfaces;

public interface ICanvasRepository
{
    Task<List<CanvasNode>> GetAllNodesAsync(CancellationToken ct = default);

    Task<CanvasNode?> GetNodeByIdAsync(int id, CancellationToken ct = default);
    Task<CanvasNode?> GetNodeByReferenceIdAsync(string referenceId, CancellationToken ct = default);

    Task AddNodesAsync(IEnumerable<CanvasNode> nodes, CancellationToken ct = default);

    Task UpdateNodeAsync(CanvasNode node, CancellationToken ct = default);

    /// <summary>Removes all nodes belonging to a playlist and their related edges (cascade via FK).</summary>
    Task RemovePlaylistNodesAsync(string playlistSpotifyId, CancellationToken ct = default);
}
