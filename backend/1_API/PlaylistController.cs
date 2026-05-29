using backend.Business.DTOs;
using backend.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.API;

[ApiController]
[Route("api/[controller]")]
public class PlaylistController(PlaylistAnalyzerService analyzerService) : ControllerBase
{
    [HttpPost("analyze")]
    public async Task<ActionResult<PlaylistResultDto>> Analyze([FromBody] AnalyzeRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("L'URL della playlist non può essere vuoto.");

        try
        {
            var result = await analyzerService.AnalyzeAsync(request.Url, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<PlaylistSummaryDto>>> GetHistory(CancellationToken ct)
    {
        var history = await analyzerService.GetHistoryAsync(ct);
        return Ok(history);
    }

    [HttpGet("{spotifyId}")]
    public async Task<ActionResult<PlaylistResultDto>> GetById(string spotifyId, CancellationToken ct)
    {
        var result = await analyzerService.GetPlaylistAsync(spotifyId, ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }
}
