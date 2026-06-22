namespace backend.Infrastructure.Spotify;

public class SpotifyOptions
{
    public const string Section = "Spotify";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
