namespace backend.Infrastructure.Spotify;

public class SpotifyOptions
{
    public const string Section = "Spotify";

    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
}
