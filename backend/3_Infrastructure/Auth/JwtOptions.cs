namespace backend.Infrastructure.Auth;

public class JwtOptions
{
    public const string Section = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "SpotifyPlaylistSampler";
    public string Audience { get; set; } = "SpotifyPlaylistSampler";
    public int ExpirationMinutes { get; set; } = 10080; // 7 days
}
