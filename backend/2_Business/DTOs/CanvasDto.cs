namespace backend.Business.DTOs;

public record CanvasNodeDto(
    int Id,
    string NodeType,
    string ReferenceId,
    string Label,
    string? Artist,
    string? ImageUrl,
    double PositionX,
    double PositionY,
    string? Color,
    string? ParentPlaylistId,
    string? ParentPlaylistName
);

public record CanvasEdgeDto(
    int Id,
    int SourceNodeId,
    int TargetNodeId,
    string EdgeType
);

public record CanvasStateDto(
    List<CanvasNodeDto> Nodes,
    List<CanvasEdgeDto> Edges
);

public record AddPlaylistToCanvasRequestDto(string SpotifyId);

public record AddTrackToCanvasRequestDto(string SpotifyId, int TrackIndex);

public record CreateEdgeRequestDto(int SourceNodeId, int TargetNodeId);

public record UpdateNodePositionDto(double PositionX, double PositionY);

public record UpdateNodePositionBatchItemDto(int Id, double PositionX, double PositionY);
