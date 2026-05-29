namespace backend.Domain;

public class CanvasNode
{
    public int Id { get; set; }

    /// <summary>"playlist" or "track"</summary>
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

    /// <summary>Hex color used for rendering (e.g. "#4CAF50").</summary>
    public string? Color { get; set; }

    /// <summary>For track nodes, the SpotifyId of the parent playlist.</summary>
    public string? ParentPlaylistId { get; set; }
}
