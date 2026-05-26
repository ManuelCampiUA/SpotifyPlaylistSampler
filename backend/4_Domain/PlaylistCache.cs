namespace backend.Domain;

/// <summary>
/// Cached Spotify playlist analysis result.
/// ResultJson holds the serialized PlaylistResultDto to keep the schema simple for v1.
/// Can be refactored to fully-normalized tables (Track, Artist, Genre) in a later iteration.
/// </summary>
public class PlaylistCache
{
    public int Id { get; set; }

    /// <summary>Spotify playlist ID (e.g. "37i9dQZF1DXcBWIGoYBM5M")</summary>
    public string SpotifyId { get; set; } = default!;

    /// <summary>Human-readable name, stored for quick history queries.</summary>
    public string Name { get; set; } = default!;

    /// <summary>When this entry was last fetched from Spotify.</summary>
    public DateTime AnalyzedAt { get; set; }

    /// <summary>Serialized PlaylistResultDto — the full analysis payload.</summary>
    public string ResultJson { get; set; } = default!;
}
