using backend.Business.DTOs;
using backend.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.API;

[ApiController]
[Route("api/[controller]")]
public class CanvasController(CanvasService canvasService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CanvasStateDto>> GetCanvas(CancellationToken ct)
    {
        var state = await canvasService.GetCanvasAsync(ct);
        return Ok(state);
    }

    [HttpPost("playlist")]
    public async Task<ActionResult<CanvasStateDto>> AddPlaylist([FromBody] AddPlaylistToCanvasRequestDto request, CancellationToken ct)
    {
        try
        {
            CanvasStateDto state = await canvasService.AddPlaylistAsync(request.SpotifyId, ct);
            return Ok(state);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("playlist/{spotifyId}")]
    public async Task<IActionResult> RemovePlaylist(string spotifyId, CancellationToken ct)
    {
        await canvasService.RemovePlaylistAsync(spotifyId, ct);
        return NoContent();
    }

    [HttpPut("nodes/{id:int}")]
    public async Task<ActionResult<CanvasNodeDto>> UpdateNodePosition(
        int id, [FromBody] UpdateNodePositionDto request, CancellationToken ct)
    {
        try
        {
            CanvasNodeDto node = await canvasService.UpdateNodePositionAsync(id, request.PositionX, request.PositionY, ct);
            return Ok(node);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
