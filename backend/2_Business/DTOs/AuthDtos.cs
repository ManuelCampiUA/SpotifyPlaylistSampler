namespace backend.Business.DTOs;

public record UserInfoDto(
    string SpotifyId,
    string DisplayName,
    string? Email,
    string? ImageUrl
);
