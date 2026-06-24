using System.Text.Json;
using backend.Business.DTOs;
using backend.Domain;
using backend.Domain.Interfaces;

namespace backend.Business.Services;

public class CanvasService(ICanvasRepository canvasRepository, IPlaylistRepository playlistRepository)
{
    private static readonly string[] Palette =
    [
        "#4CAF50", "#2196F3", "#FF9800", "#E91E63", "#9C27B0",
        "#00BCD4", "#FF5722", "#3F51B5", "#CDDC39", "#607D8B"
    ];

    private const int GridColumns = 5;
    private const double BlockStepX = 180.0; // block width (160) + gap (20)
    private const double BlockStepY = 104.0; // block height (~64) + gap (40)
    private const double CanvasPaddingX = 40.0;
    private const double CanvasPaddingY = 40.0;
    private const double PlaylistGroupPadding = 60.0;

    public async Task<CanvasStateDto> GetCanvasAsync(CancellationToken ct = default)
    {
        var nodes = await canvasRepository.GetAllNodesAsync(ct);
        var edges = await canvasRepository.GetAllEdgesAsync(ct);
        return new CanvasStateDto(
            Nodes: [.. nodes.Select(MapNodeDto)],
            Edges: [.. edges.Select(MapEdgeDto)]
        );
    }

    public async Task<CanvasStateDto> AddPlaylistAsync(string spotifyId, CancellationToken ct = default)
    {
        // Idempotency: if any block for this playlist already exists, return current state
        var all = await canvasRepository.GetAllNodesAsync(ct);
        if (all.Any(n => n.ParentPlaylistId == spotifyId))
            return await GetCanvasAsync(ct);

        PlaylistCache saved = await playlistRepository.GetBySpotifyIdAsync(spotifyId, ct)
            ?? throw new ArgumentException("Playlist non trovata. Analizzala prima dalla pagina principale.");

        PlaylistResultDto result = JsonSerializer.Deserialize<PlaylistResultDto>(saved.ResultJson)!;

        // Assign color cycling through palette based on distinct playlists already on canvas
        int playlistCount = all.Select(n => n.ParentPlaylistId).Distinct().Count(id => id is not null);
        string color = Palette[playlistCount % Palette.Length];

        // Start new playlist blocks below all existing ones
        int existingCount = all.Count;
        int existingRows = (int)Math.Ceiling(existingCount / (double)GridColumns);
        double startY = existingRows > 0
            ? CanvasPaddingY + existingRows * BlockStepY + PlaylistGroupPadding
            : CanvasPaddingY;

        var trackNodes = result.Tracks.Select((track, i) => new CanvasNode
        {
            NodeType = "track",
            ReferenceId = $"{spotifyId}:{i}",
            Label = track.Name,
            Artist = track.Artists.FirstOrDefault() ?? string.Empty,
            PositionX = CanvasPaddingX + (i % GridColumns) * BlockStepX,
            PositionY = startY + (i / GridColumns) * BlockStepY,
            Color = color,
            ParentPlaylistId = spotifyId,
            ParentPlaylistName = saved.Name
        }).ToList();

        await canvasRepository.AddNodesAsync(trackNodes, ct);

        return await GetCanvasAsync(ct);
    }

    public async Task<CanvasNodeDto> UpdateNodePositionAsync(int nodeId, double x, double y, CancellationToken ct = default)
    {
        var node = await canvasRepository.GetNodeByIdAsync(nodeId, ct)
            ?? throw new ArgumentException("Nodo non trovato.");

        node.PositionX = x;
        node.PositionY = y;
        await canvasRepository.UpdateNodeAsync(node, ct);

        return MapNodeDto(node);
    }

    public async Task<CanvasStateDto> RemovePlaylistAsync(string spotifyId, CancellationToken ct = default)
    {
        await canvasRepository.RemovePlaylistNodesAsync(spotifyId, ct);
        return await GetCanvasAsync(ct);
    }

    // ── Edges ──────────────────────────────────────────────────────

    public async Task<CanvasEdgeDto> CreateEdgeAsync(int sourceNodeId, int targetNodeId, CancellationToken ct = default)
    {
        var source = await canvasRepository.GetNodeByIdAsync(sourceNodeId, ct)
            ?? throw new ArgumentException("Nodo sorgente non trovato.");
        var target = await canvasRepository.GetNodeByIdAsync(targetNodeId, ct)
            ?? throw new ArgumentException("Nodo destinazione non trovato.");

        var edge = new CanvasEdge
        {
            SourceNodeId = sourceNodeId,
            TargetNodeId = targetNodeId,
            EdgeType = "bridge"
        };

        await canvasRepository.AddEdgeAsync(edge, ct);
        return MapEdgeDto(edge);
    }

    public async Task RemoveEdgeAsync(int edgeId, CancellationToken ct = default)
    {
        await canvasRepository.RemoveEdgeAsync(edgeId, ct);
    }

    // ── Batch position save ────────────────────────────────────────

    public async Task BatchUpdatePositionsAsync(List<UpdateNodePositionBatchItemDto> items, CancellationToken ct = default)
    {
        var ids = items.Select(i => i.Id).ToList();
        var nodes = await canvasRepository.GetNodesByIdsAsync(ids, ct);
        var lookup = items.ToDictionary(i => i.Id);

        foreach (var node in nodes)
        {
            if (lookup.TryGetValue(node.Id, out var update))
            {
                node.PositionX = update.PositionX;
                node.PositionY = update.PositionY;
            }
        }

        await canvasRepository.UpdateNodesAsync(nodes, ct);
    }

    private static CanvasNodeDto MapNodeDto(CanvasNode n) => new(
        n.Id, n.NodeType, n.ReferenceId, n.Label, n.Artist,
        n.PositionX, n.PositionY, n.Color, n.ParentPlaylistId, n.ParentPlaylistName
    );

    private static CanvasEdgeDto MapEdgeDto(CanvasEdge e) => new(
        e.Id, e.SourceNodeId, e.TargetNodeId, e.EdgeType
    );
}
