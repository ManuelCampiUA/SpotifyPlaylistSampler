namespace backend.Domain;

public class CanvasNode
{
    public int Id { get; set; }
    public string NodeType { get; set; } = default!;

    /// <summary>
    /// Unique reference within the canvas.
    /// Playlist → SpotifyId (e.g. "37i9dQZF1DXcBWIGoYBM5M").
    /// Track    → "{playlistSpotifyId}:{trackIndex}".
    /// </summary>
    public string ReferenceId { get; set; } = default!;
    public string Label { get; set; } = default!;
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public string? Color { get; set; }
    public string? ParentPlaylistId { get; set; }
}
