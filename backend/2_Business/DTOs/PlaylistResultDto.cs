namespace backend.Business.DTOs;

public record PlaylistResultDto(
    string PlaylistName,
    string? Description,
    int TotalTracks,
    string? ImageUrl,
    List<TrackDto> Tracks,
    List<string> Genres
);

public record PlaylistSummaryDto(
    string SpotifyId,
    string PlaylistName,
    int TotalTracks,
    string? ImageUrl
);

public record TrackDto(
    string Name,
    List<string> Artists,
    int? DurationMs,
    string? PreviewUrl
);
