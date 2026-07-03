using backend.Business.DTOs;
using backend.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.API;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpGet("login")]
    public IActionResult Login()
    {
        var url = authService.GetSpotifyLoginUrl();
        return Redirect(url.ToString());
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? error,
        CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
            return Redirect($"{authService.FrontendUrl}/login?error=access_denied");

        try
        {
            var jwt = await authService.HandleCallbackAsync(code, ct);
            return Redirect($"{authService.FrontendUrl}/auth/callback?token={Uri.EscapeDataString(jwt)}");
        }
        catch
        {
            return Redirect($"{authService.FrontendUrl}/login?error=auth_failed");
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserInfoDto>> Me(CancellationToken ct)
    {
        var spotifyId = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (spotifyId is null) return Unauthorized();

        var user = await authService.GetUserAsync(spotifyId, ct);
        if (user is null) return Unauthorized();

        return Ok(new UserInfoDto(user.SpotifyId, user.DisplayName, user.Email, user.ImageUrl));
    }
}
