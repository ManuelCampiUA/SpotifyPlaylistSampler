namespace backend.Domain;

public class AppUser
{
    public int Id { get; set; }
    public string SpotifyId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? Email { get; set; }
    public string? ImageUrl { get; set; }
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public DateTime TokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}
