using System.Text.Json;
using backend.Business.DTOs;
using backend.Domain;
using backend.Domain.Interfaces;

namespace backend.Business.Services;

public class CanvasService(
    ICanvasRepository canvasRepository, IPlaylistRepository playlistRepository, ICurrentUser currentUser)
{
    private static readonly string[] Palette =
    [
        "#4CAF50", "#2196F3", "#FF9800", "#E91E63", "#9C27B0",
        "#00BCD4", "#FF5722", "#3F51B5", "#CDDC39", "#607D8B"
    ];

    private const double DefaultSpawnX = 400.0;
    private const double DefaultSpawnY = 300.0;

    public async Task<CanvasStateDto> GetCanvasAsync(CancellationToken ct = default)
    {
        var userId = currentUser.SpotifyId;
        var nodes = await canvasRepository.GetAllNodesAsync(userId, ct);
        var edges = await canvasRepository.GetAllEdgesAsync(userId, ct);
        return new CanvasStateDto(
            Nodes: [.. nodes.Select(MapNodeDto)],
            Edges: [.. edges.Select(MapEdgeDto)]
        );
    }

    public async Task<CanvasNodeDto> AddPlaylistAsync(string spotifyId, CancellationToken ct = default)
    {
        var userId = currentUser.SpotifyId;

        var existing = await canvasRepository.GetNodeByReferenceIdAsync($"playlist:{spotifyId}", userId, ct);
        if (existing is not null) return MapNodeDto(existing);

        PlaylistCache saved = await playlistRepository.GetBySpotifyIdAsync(spotifyId, userId, ct)
            ?? throw new ArgumentException("Playlist non trovata. Analizzala prima dalla pagina principale.");

        PlaylistResultDto result = JsonSerializer.Deserialize<PlaylistResultDto>(saved.ResultJson)!;

        var all = await canvasRepository.GetAllNodesAsync(userId, ct);
        int playlistCount = all.Count(n => n.NodeType == "playlist");
        string color = Palette[playlistCount % Palette.Length];

        var node = new CanvasNode
        {
            UserSpotifyId = userId,
            NodeType = "playlist",
            ReferenceId = $"playlist:{spotifyId}",
            Label = saved.Name,
            ImageUrl = result.ImageUrl,
            PositionX = DefaultSpawnX + playlistCount * 60,
            PositionY = DefaultSpawnY + playlistCount * 40,
            Color = color,
            ParentPlaylistId = spotifyId,
            ParentPlaylistName = saved.Name
        };

        await canvasRepository.AddNodeAsync(node, ct);
        return MapNodeDto(node);
    }

    public async Task<CanvasNodeDto> AddTrackAsync(string spotifyId, int trackIndex, CancellationToken ct = default)
    {
        var userId = currentUser.SpotifyId;
        string refId = $"{spotifyId}:{trackIndex}";

        var existing = await canvasRepository.GetNodeByReferenceIdAsync(refId, userId, ct);
        if (existing is not null) return MapNodeDto(existing);

        PlaylistCache saved = await playlistRepository.GetBySpotifyIdAsync(spotifyId, userId, ct)
            ?? throw new ArgumentException("Playlist non trovata.");

        PlaylistResultDto result = JsonSerializer.Deserialize<PlaylistResultDto>(saved.ResultJson)!;

        if (trackIndex < 0 || trackIndex >= result.Tracks.Count)
            throw new ArgumentException("Indice traccia non valido.");

        var track = result.Tracks[trackIndex];

        var playlistNode = await canvasRepository.GetNodeByReferenceIdAsync($"playlist:{spotifyId}", userId, ct);
        string color = playlistNode?.Color ?? Palette[0];

        var all = await canvasRepository.GetAllNodesAsync(userId, ct);
        int count = all.Count;

        var node = new CanvasNode
        {
            UserSpotifyId = userId,
            NodeType = "track",
            ReferenceId = refId,
            Label = track.Name,
            Artist = track.Artists.FirstOrDefault() ?? string.Empty,
            PositionX = DefaultSpawnX + 200 + (count % 5) * 50,
            PositionY = DefaultSpawnY + (count / 5) * 60,
            Color = color,
            ParentPlaylistId = spotifyId,
            ParentPlaylistName = saved.Name
        };

        await canvasRepository.AddNodeAsync(node, ct);
        return MapNodeDto(node);
    }

    public async Task<CanvasNodeDto> UpdateNodePositionAsync(int nodeId, double x, double y, CancellationToken ct = default)
    {
        var node = await canvasRepository.GetNodeByIdAsync(nodeId, currentUser.SpotifyId, ct)
            ?? throw new ArgumentException("Nodo non trovato.");

        node.PositionX = x;
        node.PositionY = y;
        await canvasRepository.UpdateNodeAsync(node, ct);

        return MapNodeDto(node);
    }

    public async Task RemoveNodeAsync(int nodeId, CancellationToken ct = default)
    {
        await canvasRepository.RemoveNodeAsync(nodeId, currentUser.SpotifyId, ct);
    }

    public async Task ClearAllAsync(CancellationToken ct = default)
    {
        await canvasRepository.ClearAllAsync(currentUser.SpotifyId, ct);
    }

    public async Task<CanvasEdgeDto> CreateEdgeAsync(int sourceNodeId, int targetNodeId, CancellationToken ct = default)
    {
        var userId = currentUser.SpotifyId;
        var source = await canvasRepository.GetNodeByIdAsync(sourceNodeId, userId, ct)
            ?? throw new ArgumentException("Nodo sorgente non trovato.");
        var target = await canvasRepository.GetNodeByIdAsync(targetNodeId, userId, ct)
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
        await canvasRepository.RemoveEdgeAsync(edgeId, currentUser.SpotifyId, ct);
    }

    public async Task BatchUpdatePositionsAsync(List<UpdateNodePositionBatchItemDto> items, CancellationToken ct = default)
    {
        var ids = items.Select(i => i.Id).ToList();
        var nodes = await canvasRepository.GetNodesByIdsAsync(ids, currentUser.SpotifyId, ct);
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
        n.Id, n.NodeType, n.ReferenceId, n.Label, n.Artist, n.ImageUrl,
        n.PositionX, n.PositionY, n.Color, n.ParentPlaylistId, n.ParentPlaylistName
    );

    private static CanvasEdgeDto MapEdgeDto(CanvasEdge e) => new(
        e.Id, e.SourceNodeId, e.TargetNodeId, e.EdgeType
    );
}
