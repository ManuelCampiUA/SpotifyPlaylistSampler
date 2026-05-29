using System.Text.Json;
using backend.Business.DTOs;
using backend.Domain;
using backend.Domain.Interfaces;

namespace backend.Business.Services;

public class CanvasService(
    ICanvasRepository canvasRepository,
    IPlaylistRepository playlistRepository)
{
    private static readonly string[] Palette =
    [
        "#4CAF50", "#2196F3", "#FF9800", "#E91E63", "#9C27B0",
        "#00BCD4", "#FF5722", "#3F51B5", "#CDDC39", "#607D8B"
    ];

    public async Task<CanvasStateDto> GetCanvasAsync(CancellationToken ct = default)
    {
        var nodes = await canvasRepository.GetAllNodesAsync(ct);
        var edges = await canvasRepository.GetAllEdgesAsync(ct);

        return new CanvasStateDto(
            Nodes: nodes.Select(MapNodeDto).ToList(),
            Edges: edges.Select(MapEdgeDto).ToList()
        );
    }

    public async Task<CanvasStateDto> AddPlaylistAsync(string spotifyId, CancellationToken ct = default)
    {
        // Guard: don't add the same playlist twice
        var existing = await canvasRepository.GetNodeByReferenceIdAsync(spotifyId, ct);
        if (existing is not null)
            throw new InvalidOperationException("Questa playlist è già presente sulla canvas.");

        var cached = await playlistRepository.GetBySpotifyIdAsync(spotifyId, ct)
            ?? throw new ArgumentException("Playlist non trovata. Analizzala prima dalla pagina principale.");

        var result = JsonSerializer.Deserialize<PlaylistResultDto>(cached.ResultJson)!;

        // Pick a color based on how many playlists are already on canvas
        var playlistCount = (await canvasRepository.GetAllNodesAsync(ct))
            .Count(n => n.NodeType == "playlist");
        var color = Palette[playlistCount % Palette.Length];

        // Create playlist node at a staggered position
        var playlistNode = new CanvasNode
        {
            NodeType = "playlist",
            ReferenceId = spotifyId,
            Label = result.PlaylistName,
            PositionX = 300 + playlistCount * 400,
            PositionY = 300,
            Color = color,
            ParentPlaylistId = null
        };

        var trackNodes = result.Tracks.Select((track, i) =>
        {
            var angle = 2 * Math.PI * i / result.Tracks.Count;
            var radius = 120 + result.Tracks.Count * 3;
            return new CanvasNode
            {
                NodeType = "track",
                ReferenceId = $"{spotifyId}:{i}",
                Label = track.Name,
                PositionX = playlistNode.PositionX + radius * Math.Cos(angle),
                PositionY = playlistNode.PositionY + radius * Math.Sin(angle),
                Color = color,
                ParentPlaylistId = spotifyId
            };
        }).ToList();

        var allNodes = new List<CanvasNode> { playlistNode };
        allNodes.AddRange(trackNodes);
        await canvasRepository.AddNodesAsync(allNodes, ct);

        // Create intra-playlist edges (each track ↔ playlist node)
        var edges = trackNodes.Select(t => new CanvasEdge
        {
            SourceNodeId = playlistNode.Id,
            TargetNodeId = t.Id,
            EdgeType = "intra-playlist"
        }).ToList();

        await canvasRepository.AddEdgesAsync(edges, ct);

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

    public async Task<CanvasEdgeDto> CreateEdgeAsync(int sourceNodeId, int targetNodeId, CancellationToken ct = default)
    {
        if (sourceNodeId == targetNodeId)
            throw new ArgumentException("Non puoi collegare un nodo a se stesso.");

        var source = await canvasRepository.GetNodeByIdAsync(sourceNodeId, ct)
            ?? throw new ArgumentException("Nodo sorgente non trovato.");
        var target = await canvasRepository.GetNodeByIdAsync(targetNodeId, ct)
            ?? throw new ArgumentException("Nodo destinazione non trovato.");

        var edge = await canvasRepository.AddEdgeAsync(new CanvasEdge
        {
            SourceNodeId = sourceNodeId,
            TargetNodeId = targetNodeId,
            EdgeType = "custom"
        }, ct);

        return MapEdgeDto(edge);
    }

    public Task RemoveEdgeAsync(int edgeId, CancellationToken ct = default)
        => canvasRepository.RemoveEdgeAsync(edgeId, ct);

    public async Task<CanvasStateDto> RemovePlaylistAsync(string spotifyId, CancellationToken ct = default)
    {
        await canvasRepository.RemovePlaylistNodesAsync(spotifyId, ct);
        return await GetCanvasAsync(ct);
    }

    private static CanvasNodeDto MapNodeDto(CanvasNode n) => new(
        n.Id, n.NodeType, n.ReferenceId, n.Label,
        n.PositionX, n.PositionY, n.Color, n.ParentPlaylistId
    );

    private static CanvasEdgeDto MapEdgeDto(CanvasEdge e) => new(
        e.Id, e.SourceNodeId, e.TargetNodeId, e.EdgeType
    );
}
