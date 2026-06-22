namespace backend.Business.DTOs;

public record CanvasNodeDto(
    int Id,
    string NodeType,
    string ReferenceId,
    string Label,
    string? Artist,
    double PositionX,
    double PositionY,
    string? Color,
    string? ParentPlaylistId
);

public record CanvasStateDto(
    List<CanvasNodeDto> Nodes
);

public record AddPlaylistToCanvasRequestDto(string SpotifyId);

public record UpdateNodePositionDto(double PositionX, double PositionY);
