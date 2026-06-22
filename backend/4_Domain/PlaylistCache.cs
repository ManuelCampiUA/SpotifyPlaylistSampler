namespace backend.Domain;

/// <summary>
/// Cached Spotify playlist analysis result.
/// ResultJson holds the serialized PlaylistResultDto to keep the schema simple for v1.
/// Can be refactored to fully-normalized tables (Track, Artist, Genre) in a later iteration.
/// </summary>
public class PlaylistCache
{
    public int Id { get; set; }

    public string SpotifyId { get; set; } = default!;

    public string Name { get; set; } = default!;

    public DateTime AnalyzedAt { get; set; }

    public string ResultJson { get; set; } = default!;
}
