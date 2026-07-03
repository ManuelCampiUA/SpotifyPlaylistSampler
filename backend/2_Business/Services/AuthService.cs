using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Domain;
using backend.Domain.Interfaces;
using backend.Infrastructure.Auth;
using backend.Infrastructure.Spotify;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SpotifyAPI.Web;

namespace backend.Business.Services;

public class AuthService(
    IOptions<SpotifyOptions> spotifyOptions,
    IOptions<JwtOptions> jwtOptions,
    IUserRepository userRepository,
    IConfiguration configuration)
{
    private readonly SpotifyOptions _spotify = spotifyOptions.Value;
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public string FrontendUrl =>
        configuration["FrontendUrl"] ?? "http://localhost:4200";

    public Uri GetSpotifyLoginUrl()
    {
        var request = new LoginRequest(
            new Uri(_spotify.RedirectUri),
            _spotify.ClientId,
            LoginRequest.ResponseType.Code)
        {
            Scope =
            [
                Scopes.PlaylistReadPrivate,
                Scopes.PlaylistReadCollaborative,
                Scopes.UserReadEmail,
                Scopes.UserReadPrivate
            ]
        };

        return request.ToUri();
    }

    public async Task<string> HandleCallbackAsync(string code, CancellationToken ct)
    {
        var tokenResponse = await new OAuthClient().RequestToken(
            new AuthorizationCodeTokenRequest(
                _spotify.ClientId,
                _spotify.ClientSecret,
                code,
                new Uri(_spotify.RedirectUri)));

        var spotify = new SpotifyClient(tokenResponse.AccessToken);
        var profile = await spotify.UserProfile.Current(ct);

        var user = await userRepository.GetBySpotifyIdAsync(profile.Id, ct);

#pragma warning disable CS0618 // Email may still be available for some users
        var email = profile.Email;
#pragma warning restore CS0618

        if (user is null)
        {
            user = new AppUser
            {
                SpotifyId = profile.Id,
                DisplayName = profile.DisplayName ?? profile.Id,
                Email = email,
                ImageUrl = profile.Images?.FirstOrDefault()?.Url,
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken ?? string.Empty,
                TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
        }
        else
        {
            user.DisplayName = profile.DisplayName ?? profile.Id;
            user.Email = email;
            user.ImageUrl = profile.Images?.FirstOrDefault()?.Url;
            user.AccessToken = tokenResponse.AccessToken;
            user.RefreshToken = tokenResponse.RefreshToken ?? user.RefreshToken;
            user.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            user.LastLoginAt = DateTime.UtcNow;
        }

        await userRepository.SaveAsync(user, ct);

        return GenerateJwt(user);
    }

    public Task<AppUser?> GetUserAsync(string spotifyId, CancellationToken ct)
        => userRepository.GetBySpotifyIdAsync(spotifyId, ct);

    private string GenerateJwt(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.SpotifyId),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new("picture", user.ImageUrl ?? string.Empty),
        };

        if (user.Email is not null)
            claims.Add(new(JwtRegisteredClaimNames.Email, user.Email));

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
