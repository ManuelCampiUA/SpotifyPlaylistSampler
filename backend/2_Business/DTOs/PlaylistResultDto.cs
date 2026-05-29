namespace backend.Business.DTOs;

/// <summary>
/// Full analysis result returned to the frontend.
/// Property names are serialized as camelCase by ASP.NET Core by default,
/// matching the Angular model exactly.
/// </summary>
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
    string Name,
    DateTime AnalyzedAt
);

public record TrackDto(
    string Name,
    List<string> Artists,
    int? DurationMs,
    string? PreviewUrl
);
