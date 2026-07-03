using backend.Business.DTOs;
using backend.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.API;

[Authorize]
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
    public async Task<ActionResult<CanvasNodeDto>> AddPlaylist([FromBody] AddPlaylistToCanvasRequestDto request, CancellationToken ct)
    {
        try
        {
            var node = await canvasService.AddPlaylistAsync(request.SpotifyId, ct);
            return Ok(node);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("track")]
    public async Task<ActionResult<CanvasNodeDto>> AddTrack([FromBody] AddTrackToCanvasRequestDto request, CancellationToken ct)
    {
        try
        {
            var node = await canvasService.AddTrackAsync(request.SpotifyId, request.TrackIndex, ct);
            return Ok(node);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("nodes/{id:int}")]
    public async Task<IActionResult> RemoveNode(int id, CancellationToken ct)
    {
        await canvasService.RemoveNodeAsync(id, ct);
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

    [HttpPut("nodes/batch")]
    public async Task<IActionResult> BatchUpdatePositions(
        [FromBody] List<UpdateNodePositionBatchItemDto> items, CancellationToken ct)
    {
        await canvasService.BatchUpdatePositionsAsync(items, ct);
        return NoContent();
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearAll(CancellationToken ct)
    {
        await canvasService.ClearAllAsync(ct);
        return NoContent();
    }

    [HttpPost("edges")]
    public async Task<ActionResult<CanvasEdgeDto>> CreateEdge(
        [FromBody] CreateEdgeRequestDto request, CancellationToken ct)
    {
        try
        {
            var edge = await canvasService.CreateEdgeAsync(request.SourceNodeId, request.TargetNodeId, ct);
            return Ok(edge);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("edges/{id:int}")]
    public async Task<IActionResult> RemoveEdge(int id, CancellationToken ct)
    {
        await canvasService.RemoveEdgeAsync(id, ct);
        return NoContent();
    }
}
